namespace OptionPricing.Core.Engines;

using System;
using System.Collections.Generic;
using System.Linq;
using OptionPricing.Core.Models;
using OptionPricing.Core.Engines.Rules;

public sealed class EngineFactory
{
    public sealed record Options(
        EngineAccuracy Accuracy = EngineAccuracy.Balanced,
        int? Steps = null,
        int? Paths = null,
        int? PriceSteps = null,
        int? BasisDegree = null
    );

    // NEW: metadata about the last selection
    public sealed record Selection(string EngineName, string RuleName);

    // NEW: inspect after Create(...)
    public Selection? LastSelection { get; private set; }

    private readonly List<IEngineRule> _rules = new();

    public EngineFactory Register(IEngineRule rule)
    {
        if (rule is null) throw new ArgumentNullException(nameof(rule));
        _rules.Add(rule);
        return this;
    }

    public IPricingEngine Create(Option option, Options? cfg = null)
    {
        if (option is null) throw new ArgumentNullException(nameof(option));
        cfg ??= new Options();

        var winner = _rules
            .Where(r => r.Matches(option))
            .OrderByDescending(r => r.Priority)
            .FirstOrDefault();

        if (winner is null)
            throw new InvalidOperationException(
                "No pricing rule matched this option. Register a rule for this instrument."
            );

        var engine = winner.Build(option, cfg);

        // NEW: store selection info (no engine/rule changes required)
        var engineName = engine.GetType().Name;      // e.g., "FiniteDifferencePricingEngine"
        var ruleName   = winner.GetType().Name;      // e.g., "AmericanVanillaRule"
        LastSelection  = new Selection(engineName, ruleName);

        return engine;
    }

    public static EngineFactory Default { get; } = new EngineFactory();
}