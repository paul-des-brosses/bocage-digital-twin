# Bocage Digital Twin

*A digital twin of a Norman bocage countryside, instrumented and resilient.*

**▶ [Open the live demo](https://paul-des-brosses.github.io/bocage-digital-twin/)** — runs in the browser (WebGL, desktop ≥ 1280px).

<!--
  HERO MEDIA — insert a 10-15s GIF (or MP4) of the running demo showing the
  scene + dashboard + one preset change. Drop the file under docs/media/ and
  reference it here, e.g.:
  ![Bocage Digital Twin — live demo](docs/media/hero.gif)
-->

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

<!--
  SCREENSHOT — the full UI in dark mode. Drop the file under docs/media/ and
  reference it here, e.g.:  ![Full dashboard in dark mode](docs/media/ui-dark.png)
-->

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
  user to arbitrate (e.g. activate auxiliary irrigation under prolonged
  drought, reduce input intensity after a fauna acoustic anomaly). Three
  manual "punctual interventions" mirror the same actions for the farmer
  to trigger them off-event — all of them journalled and audited through
  the same `DecisionJournal` (ADR #47).
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

See [ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full architectural
diagram and module description.

## Status

**Feature-complete MVP (v1.0).** The simulation runs end to end: five
sensor chains — weather station, eddy-covariance tower, piezometer,
acoustic and camera-trap fauna sensors — feed measured variables all the
way to the displayed indicators, the recommendations and the visible
fauna. No visual element is driven by the calendar: every variation traces
back to a sensor measurement or a model variable (the *sensor primacy*
rule, see [CLAUDE.md](CLAUDE.md) §9).

Delivered across the roadmap's worksites (E1-E11, see [docs/ROADMAP.md](docs/ROADMAP.md)):
the five-layer architecture and core simulation, seasonal weather (Markov
rain on Mortagne-au-Perche normals), a one-pool soil-carbon model, four
biodiversity-driven fauna species, an investment / profitability horizon,
clickable per-sensor inspection panels with time-series charts, and the
three Level-B thematic panels (biodiversity, climate & resources, economy).
The latest worksite makes the decision layer fully **model-derived**: each
recommendation and its outcome projection come from simulating the lever
forward on a copy of the state (no fixed coefficients), and the drought and
soil-carbon alerts now threshold the sensors' own noisy readings rather than
the model's ground truth. A comprehensive **EditMode test suite** covers the
simulation core, the indicators, the recommendation journal and the sensor
noise models.

A scientific overview of what is simulated, with its sources, lives in
[docs/SIMULATION_OVERVIEW.md](docs/SIMULATION_OVERVIEW.md). Items
deliberately deferred past v1 are tracked, each with implementation hooks,
in [docs/BACKLOG.md](docs/BACKLOG.md).

## Getting started

**Unity version required**: `6000.4.4f1` (Unity 6 LTS). Open the project
folder from Unity Hub — the editor will warn if the installed version
differs.

**Open the scene**: `Assets/_Project/Main.unity`. This is the only scene
in the project (cf. DECISIONS #25). Press Play.

**Run the tests**:
`Window > General > Test Runner > EditMode > Run All`. The suite covers
the simulation core, indicators, the recommendation engine, the journal
supersession logic and the sensor noise model. All tests should be
green on a fresh clone.

**Project reference docs** (under `docs/`):
- [`CLAUDE.md`](CLAUDE.md) — operational specification (hard rules,
  layer contracts, conventions).
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 5-layer
  architecture detail, asmdef graph, data flow.
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — 62 ADRs covering every
  significant design choice with rationale and sources.
- [`docs/CALIBRATION.md`](docs/CALIBRATION.md) — calibration of every
  numerical parameter (Solagro, INRAE, PNR Perche, Légifrance…).
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — step-by-step delivery
  history and remaining items.
- [`docs/BACKLOG.md`](docs/BACKLOG.md) — items deliberately deferred
  past v1, each with implementation hooks.
- [`docs/SCENE_WIRING.md`](docs/SCENE_WIRING.md) — which binding sits
  on which scene root, which ScriptableObject is wired where.
- [`docs/WEBGL_GOTCHAS.md`](docs/WEBGL_GOTCHAS.md) — known WebGL
  pitfalls already mitigated in code.

## Method

This project was designed and developed leveraging modern AI tooling to
accelerate production:

- **Architecture and design decisions**: developed iteratively in
  collaboration with Claude (claude.ai), documented in
  [DECISIONS.md](docs/DECISIONS.md).
- **Implementation**: developed with Claude Code, following the
  architectural specification in [CLAUDE.md](CLAUDE.md).
- **Visual assets**: 2D sprites generated via Nanobanana with stylistic
  consistency through ip-adapter style references and a Python
  post-processing pipeline.
- **Calibration and design judgment**: human-driven, based on data from
  Solagro, INRAE, Efese, and the Perche PNR.

The architecture, technical trade-offs, and the scientific calibration of
the simulation rules are human decisions, documented in
[DECISIONS.md](docs/DECISIONS.md) and [ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

*Optimized for desktop screens ≥ 1280px wide. Mobile experience is not
supported.*
