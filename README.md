The Option Pricing API is a modular C# .NET 8–based framework for valuing derivative securities and exposing them through a web service. At its core, it models options in terms of three simple building blocks: an exercise style, a payoff type, and a pricing engine. Each of these is represented by an interface and a set of concrete strategies, so extending the system is as easy as writing a new class. Add a new payoff (e.g., digital, barrier), drop in a new exercise type, or implement a new numerical engine, and the system immediately supports it without changing existing code. The system is designed to be modular at its core while also being fundamentally SOLID.

The design emphasizes engine modularity. The project currently includes four engines:

1. A Binomial Tree Engine, with step-based backward induction,

2. A Finite Difference Engine, using a Crank–Nicholson scheme for PDE valuation,

3. A standard Monte Carlo Engine for path-dependent payoffs like Asian options, and

4. A Longstaff–Schwartz Monte Carlo (LSMC) Engine for American options, using regression to decide optimal exercise.

Because engines all implement a common interface, swapping methods is trivial, and each engine can be tuned with parameters like steps, paths, or basis degree.

At the orchestration level, an Engine Factory uses a set of configurable rules to decide which engine is most appropriate for a given option. Rules are modular objects themselves: you can register as many as you like, in any order, and the factory will apply them in sequence. Out of the box, rules handle path-dependent payoffs, American vs. European styles, and provide fallback choices, but adding your own rule is just another class definition. This makes the system highly adaptable: as a developer you can override defaults, introduce custom heuristics, or enforce institution-specific logic with minimal effort.

On top of the Core library, the API layer exposes the pricing functionality as REST endpoints (/price, /greeks) that accept all model inputs as query parameters. A lightweight frontend served directly from the API provides an HTML/JavaScript form for entering option parameter. This system can work as a backend service for integration into larger trading or risk platforms, or as an interactive educational tool.
