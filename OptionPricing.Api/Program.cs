using OptionPricing.Core.Engines;
using OptionPricing.Core.Engines.Rules;
using OptionPricing.Core.Models;
using OptionPricing.Core.Strategies.Exercise;
using OptionPricing.Core.Strategies.Payoff;

var builder = WebApplication.CreateBuilder(args);

// Configure your factory once
EngineFactoryBootstrap.ConfigureDefault(factory => factory
    .Register(new PathDependentRule())
    .Register(new AmericanVanillaRule())
    .Register(new EuropeanVanillaRule())
    .Register(new FallbackRule())
);
var factory = EngineFactory.Default;

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
// Health
app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));
app.MapGet("/docs", () => Results.Redirect("/docs.html"));

// GET /price?S=100&K=100&T=1&R=0.05&Sigma=0.2&Q=0.02&exercise=european&payoff=call&engine=auto&steps=1000&paths=20000
app.MapGet("/price", (
    double S,
    double K,
    double T,
    double R,
    double Sigma,
    double Q,
    string exercise,               // "european" | "american"
    string payoff,                 // "call" | "put" | "asian_call" | "asian_put"
    string? engine,                // optional: "auto" | "binomial" | "fdm" | "mc"
    int? steps,                    // optional (binomial/fdm/mc)
    int? paths,                     // optional (mc)
    int? basisDegree               // optional (lsmc)
) =>
{
    // 1) Parse inputs to internal enums
    if (!Parsers.TryParseExercise(exercise, out var exType))
        return Results.BadRequest(new { error = $"Invalid exercise='{exercise}'. Use 'european' or 'american'." });

    if (!Parsers.TryParsePayoff(payoff, out var poType))
        return Results.BadRequest(new { error = $"Invalid payoff='{payoff}'. Use 'call'|'put'|'asian_call'|'asian_put'." });

    if (!Parsers.TryParseEngine(engine ?? "auto", out var engType))
        return Results.BadRequest(new { error = $"Invalid engine='{engine}'. Use 'auto'|'binomial'|'fdm'|'mc'." });

    var stps = steps ?? 1000;
    var pths = paths ?? 20000;
    var bdeg = basisDegree ?? 2;

    // 2) Map to Core types
    IExercise exerciseObj = exType switch
    {
        ExerciseType.European => new EuropeanExercise(),
        ExerciseType.American => new AmericanExercise(),
        _ => throw new InvalidOperationException()
    };

    IPayoff payoffObj = poType switch
    {
        PayoffType.Call => new CallPayoff(K),
        PayoffType.Put => new PutPayoff(K),
        PayoffType.AsianCall => new AsianCallPayoff(K),
        PayoffType.AsianPut => new AsianPutPayoff(K),
        _ => throw new InvalidOperationException()
    };

    // guardrails
    var isAsian = poType is PayoffType.AsianCall or PayoffType.AsianPut;
    if (isAsian && engType == EngineType.Binomial)
        return Results.BadRequest(new { error = "Binomial engine cannot price Asian (path-dependent) payoffs. Use 'mc' or 'auto'." });

    if (exType == ExerciseType.American && engType == EngineType.Mc)
        return Results.BadRequest(new { error = "Monte Carlo engine does not support American exercise. Use 'binomial', 'lsmc', 'fdm', or 'auto'." });

    if (isAsian && engType == EngineType.Lsmc)
    return Results.BadRequest(new { error = "LSMC engine here assumes vanilla (non-path-dependent) payoff. Use 'mc' for Asian, or 'auto'." });

    // 3) Build option & pick engine
    var option = new Option(S, K, T, R, Sigma, Q, exerciseObj, payoffObj);

    IPricingEngine engineObj = engType switch
    {
        EngineType.Auto => factory.Create(option, new EngineFactory.Options(EngineAccuracy.Accurate)),
        EngineType.Binomial => new BinomialTreePricingEngine(stps),
        EngineType.Fdm => new FiniteDifferencePricingEngine(stps),
        EngineType.Mc => new MonteCarloPricingEngine(steps: stps, paths: pths),
        EngineType.Lsmc     => new LongstaffSchwartzPricingEngine(steps: stps, paths: pths, basisDegree: bdeg),
        _ => factory.Create(option, new EngineFactory.Options(EngineAccuracy.Accurate))
    };

    // 4) Price
    double price;
    try
    {
        price = engineObj.Price(option);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    // 5) Respond
    return Results.Ok(new PriceResult(
        Price: price,
        EngineUsed: engineObj.GetType().Name,
        RulePicked: factory.LastSelection?.RuleName ?? (engType == EngineType.Auto ? "unknown" : "manual/override"),
        Inputs: new PriceInputs(S, K, T, R, Sigma, Q, exType, poType, engType, stps, pths, bdeg)
    ));
});


// GET /greeks?S=...&K=...&T=...&R=...&Sigma=...&Q=...&exercise=...&payoff=...
app.MapGet("/greeks", (
    double S,
    double K,
    double T,
    double R,
    double Sigma,
    double Q,
    string exercise,   // "european" | "american"
    string payoff      // "call" | "put" | "asian_call" | "asian_put"
) =>
{
    // Parse inputs
    if (!Parsers.TryParseExercise(exercise, out var exType))
        return Results.BadRequest(new { error = $"Invalid exercise='{exercise}'. Use 'european' or 'american'." });

    if (!Parsers.TryParsePayoff(payoff, out var poType))
        return Results.BadRequest(new { error = $"Invalid payoff='{payoff}'. Use 'call'|'put'|'asian_call'|'asian_put'." });

    // Map to Core objects
    IExercise exerciseObj = exType switch
    {
        ExerciseType.European => new EuropeanExercise(),
        ExerciseType.American => new AmericanExercise(),
        _ => throw new InvalidOperationException()
    };

    IPayoff payoffObj = poType switch
    {
        PayoffType.Call      => new CallPayoff(K),
        PayoffType.Put       => new PutPayoff(K),
        PayoffType.AsianCall => new AsianCallPayoff(K),
        PayoffType.AsianPut  => new AsianPutPayoff(K),
        _ => throw new InvalidOperationException()
    };

    var option = new Option(S, K, T, R, Sigma, Q, exerciseObj, payoffObj);

    // Use Core Greeks exactly like your console demo (no custom factory injection)
    var delta = new OptionPricing.Core.Greeks.Delta();
    var gamma = new OptionPricing.Core.Greeks.Gamma();
    var vega  = new OptionPricing.Core.Greeks.Vega();
    var theta = new OptionPricing.Core.Greeks.Theta();
    var rho   = new OptionPricing.Core.Greeks.Rho();

    try
    {
        double d = delta.Compute(option);
        double g = gamma.Compute(option);
        double v = vega.Compute(option);
        double t = theta.Compute(option);
        double r = rho.Compute(option);
        return Results.Ok(new GreeksResult(
            Delta: d, Gamma: g, Vega: v, Theta: t, Rho: r,
            EngineUsed: "Factory.Default (per Core rules)",
            Inputs: new PriceInputs(S, K, T, R, Sigma, Q, exType, poType, EngineType.Auto, 0, 0)
        ));
    }
    catch (NotImplementedException nie)
    {
        return Results.BadRequest(new { error = nie.Message });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});


app.Run();


// ========== TYPES BELOW (keeps top-level clean) ==========

public enum ExerciseType { European, American }
public enum PayoffType   { Call, Put, AsianCall, AsianPut }
public enum EngineType   { Auto, Binomial, Fdm, Mc, Lsmc }

public record PriceInputs(
    double S, double K, double T, double R, double Sigma, double Q,
    ExerciseType Exercise, PayoffType Payoff, EngineType Engine,
    int Steps, int Paths, int BasisDegree = 2  // only for LSMC
);

public record PriceResult(
    double Price,
    string EngineUsed,
    string? RulePicked,
    PriceInputs Inputs
);

public static class Parsers
{
    public static bool TryParseExercise(string s, out ExerciseType type)
    {
        switch ((s ?? "").Trim().ToLowerInvariant())
        {
            case "european": type = ExerciseType.European; return true;
            case "american": type = ExerciseType.American; return true;
            default: type = default; return false;
        }
    }

    public static bool TryParsePayoff(string s, out PayoffType type)
    {
        switch ((s ?? "").Trim().ToLowerInvariant())
        {
            case "call":        type = PayoffType.Call; return true;
            case "put":         type = PayoffType.Put; return true;
            case "asian_call":  type = PayoffType.AsianCall; return true;
            case "asian_put":   type = PayoffType.AsianPut; return true;
            default: type = default; return false;
        }
    }

    public static bool TryParseEngine(string s, out EngineType type)
    {
        switch ((s ?? "").Trim().ToLowerInvariant())
        {
            case "auto":     type = EngineType.Auto; return true;
            case "binomial": type = EngineType.Binomial; return true;
            case "fdm":      type = EngineType.Fdm; return true;
            case "mc":       type = EngineType.Mc; return true;
            case "lsmc":     type = EngineType.Lsmc; return true;
            default: type = default; return false;
        }
    }
}

public record GreeksResult(
    double Delta, double Gamma, double Vega, double Theta, double Rho,
    string EngineUsed,
    PriceInputs Inputs
);
