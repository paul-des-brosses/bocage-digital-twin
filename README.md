# Bocage Digital Twin

*A digital twin of a Norman bocage countryside, instrumented and resilient.*

[TODO: live demo link → https://[username].github.io/[repo-name]/]

[TODO: hero GIF or screenshot of the running demo, 10-15s capture showing
scene + dashboard + a preset change]

---

## Quick pitch

This project is a digital twin of an instrumented Norman bocage landscape,
located in the Perche regional natural park. It simulates how sensor-driven
algorithmic decisions help maintain a fragile centuries-old agro-forestry
mosaic facing climatic and agricultural pressures.

The simulation honestly tests whether ecological and economic returns
converge under different management scenarios — it doesn't postulate the
answer.

## What it shows

[TODO: screenshot of the full UI in dark mode]

The interface displays a single live scene of a fictional but plausible
Perche bocage site with:

- **5 Hero KPIs**: hedgerow density (m/ha), composite biodiversity index,
  groundwater table level, integrated profitability (€/ha/year), and the
  delta of instrumented management vs. uninstrumented.
- **3 thematic panels**: Biodiversity, Climate & Resources, Economy.
- **A vector minimap** showing all sensor positions with synchronized
  hover/highlights on the scene.
- **A scenario panel** with sliders for climate, agricultural pressure,
  regulatory constraints, and time horizon.
- **A decisions panel** that surfaces algorithmic recommendations for the
  user to arbitrate (e.g. replant after chalara detection, activate
  auxiliary irrigation under prolonged drought).
- **A comparison view** showing the simulation with and without
  instrumented management, side by side.

## Tech stack

- **Engine**: Unity 6 LTS, URP 2D Renderer
- **Language**: C# (.NET Standard 2.1)
- **Architecture**: 5-layer separation (Simulation Core / Sensors /
  Decision / Indicators / Presentation), enforced via Assembly Definitions
- **Data flow**: ScriptableObjects-based reactive containers + EventBus
  for ponctual events
- **Rendering**: 2D flat-color illustration with custom Shader Graph
  shaders (sky, prairie, hedgerows, pond)
- **Build target**: WebGL, Brotli compression, IL2CPP with high stripping
- **Deployment**: GitHub Pages via GitHub Actions (game-ci/unity-builder)
- **Testing**: Unity Test Framework (EditMode unit tests on Simulation Core)

## How it works

Two simulations run in parallel, sharing the same random seeds and user
inputs:

- The **real run** applies algorithmic recommendations and automatic
  countermeasures to the ecosystem state.
- The **shadow run** ignores these and simulates the same context without
  instrumented management.

The comparison of the two runs over months or years of simulated time
reveals the actual contribution of the instrumentation, not assumed.

The simulation operates on a fixed tick rate of 1 simulated day per tick.
The user can run at x1 or x10 speed, with a "skip to end" button beyond
that. Scenario parameter changes apply via interpolated transitions over
~7-14 simulated days, never abrupt.

Visual elements are strictly driven by simulated sensor measurements and
model variables — no decorative effects tied to the calendar or to scripted
animations. This is a digital twin, not a game.

## Scientific basis

Calibration data drawn from public sources:

- [Solagro](https://solagro.org) — hedgerow valuation, agroecological
  parameters
- [INRAE](https://www.inrae.fr) — agroforestry research, bocage dynamics
- [Efese](https://www.ecologie.gouv.fr/evaluation-francaise-des-ecosystemes-et-des-services-ecosystemiques-efese)
  — French ecosystem services monetization
- [PNR du Perche](https://www.parc-naturel-perche.fr) — site context,
  characteristic species and tree composition
- MAEC (Mesures Agro-Environnementales et Climatiques) — public agricultural
  policy parameters

The simulation models a fictional but plausible Perche site. Species, tree
species, climatic pressures (chalara, droughts since 2018-2022), and
typical hedgerow density ranges (60-130 m/ha) are calibrated to be
recognizable to a Perche PNR agent or a French agro-ecologist.

## Project structure

```
Assets/_Project/
├── 01_SimulationCore/    Pure C# ecosystem model and rules
├── 02_Sensors/           Simulated instrumentation layer
├── 03_Decision/          Recommendation engine and outcome projector
├── 04_Indicators/        Hero KPIs, panels, shadow runner, reporter
├── 05_Presentation/      Unity scene, UI, bindings
├── Data/                 ScriptableObjects (runtime containers, presets,
│                         calibration data, palette)
├── Events/               EventBus and event classes
├── Tests/EditMode/       Unit tests on Simulation Core
└── Editor/, Prefabs/, Fonts/, Resources/
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full architectural diagram
and module description.

## Method

This project was designed and developed leveraging modern AI tooling to
accelerate production:

- **Architecture and design decisions**: developed iteratively in
  collaboration with Claude (claude.ai), documented in
  [DECISIONS.md](DECISIONS.md).
- **Implementation**: developed with Claude Code, following the
  architectural specification in [CLAUDE.md](CLAUDE.md).
- **Visual assets**: 2D sprites generated via Nanobanana with stylistic
  consistency through ip-adapter style references and a Python
  post-processing pipeline.
- **Calibration and design judgment**: human-driven, based on data from
  Solagro, INRAE, Efese, and the Perche PNR.

The architecture, technical trade-offs, and the scientific calibration of
the simulation rules are human decisions, documented in
[DECISIONS.md](DECISIONS.md) and [ARCHITECTURE.md](ARCHITECTURE.md).

---

*Optimized for desktop screens ≥ 1280px wide. Mobile experience is not
supported.*
