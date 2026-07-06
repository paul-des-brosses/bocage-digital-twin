# DECISIONS.md — Decision log

Log of design decisions made during the exploration phase. Light ADR
(Architecture Decision Record) format. One entry = one settled decision.
To be updated as the project progresses if a decision is revised.

---

### 1. Project subject: digital twin of the Norman Perche bocage

**Context**: choosing a biome or simulation object consistent with the
portfolio profile (Creative Technology, Ardanti R&D) and accessible in
terms of documentation.

**Decision**: digital twin of an instrumented Norman Perche bocage.

**Rationale**: subject rich in measurable ecosystem services, strong
territorial anchoring (PNR du Perche), publicly available data
(Solagro, INRAE, Efese, MAEC), current agroecological relevance.

**Rejected alternative**: instrumented coral reef — too far from the
French context, less accessible data, less distinctive portfolio signal.

---

### 2. Visual format: minimalist fixed 2D plan

**Context**: choose a format compatible with a WebGL portfolio, legible
and achievable within a constrained timeframe.

**Decision**: minimalist fixed 2D plan, strictly immobile camera.

**Rationale**: maximizes indicator legibility, avoids drift toward game
territory, sustainable scope, consistent with a dashboard UI.

**Rejected alternative**: 3D top-down or 2.5D — asset production cost and
shader/perf complexity disproportionate for a portfolio.

---

### 3. Visual style: Charles Harper + A Short Hike + Perche half-timbering

**Context**: position the project between scientific rigor and visual
warmth to avoid the "cold dashboard" rendering.

**Decision**: Charles Harper spirit (flat geometric shapes, controlled
palette), A Short Hike warmth (soft lighting, cozy atmosphere),
inspiration from the half-timbered architecture of the Perche
(muted ochre-brown-green palette).

**Rationale**: naturalist credibility without austerity, distinctive
visual signature for a portfolio, strong territorial anchoring.

**Rejected alternative**: clean high-tech style (too generic), pixel
art (too playful), photorealism (out of production scope).

---

### 4. UI mode: scientific editorial dark mode

**Context**: choose an interface mode consistent with the visual identity
and comfortable for long reading.

**Decision**: scientific editorial dark mode, validated after generating
a reference image.

**Rationale**: consistent with the observatory / research station
aesthetic, high contrast for legibility of figures, distinctive in a
portfolio.

**Rejected alternative**: paper light mode — less distinctive, lower
contrast on colored visualizations.

---

### 5. Typography: EB Garamond + JetBrains Mono

**Context**: establish the editorial identity and ensure legibility of
figures.

**Decision**: EB Garamond for titles and labels, JetBrains Mono (or IBM
Plex Mono) for numeric values.

**Rationale**: Garamond evokes serious editorial science; modern mono for
numeric precision. A legible and distinctive pairing.

**Rejected alternative**: uniform modern sans-serif — visually
mundane, no typographic hierarchy.

---

### 6. Ideological framing: moderate techno-optimism + agroecological realism

**Context**: avoid the political trap of a polarizing project while
defending a clear thesis.

**Decision**: moderate techno-optimism combined with agroecological
realism. No overt "green growth" or "degrowth" stance.

**Rationale**: scientifically defensible thesis, broad portfolio audience,
political neutrality without being lukewarm.

**Rejected alternative**: militant stance (pro or anti) — needlessly
splits the portfolio audience.

---

### 7. Calibration level: medium, real Solagro/INRAE/Efese/MAEC figures

**Context**: balance scientific rigor and production feasibility.

**Decision**: medium-level calibration, based on real public figures.
No forcing of the outcome.

**Rationale**: credibility with an agroecologist or a PNR officer without
aiming for a scientific publication.

**Rejected alternative**: ultra-rigorous calibration like an INRAE model —
out of scope; purely invented calibration — loss of credibility.

---

### 8. Economic and ecological indicators in parallel, never opposed

**Context**: pedagogical risk of opposing economy and ecology in a
Manichean way.

**Decision**: economic and ecological indicators displayed in parallel.
Profitability integrated as a central KPI (€/ha/year including monetized
ecosystem services).

**Rationale**: reflects the thesis of possible convergence, avoids the
caricature, pedagogically more accurate.

**Rejected alternative**: display a single "overall performance" axis —
masks the trade-offs.

---

### 9. Comparative with/without tech tab: parallel shadow simulation

**Context**: how to demonstrate the contribution of instrumentation
without postulating it.

**Decision**: parallel shadow simulation, same seeds and same inputs,
without applying the tech actions.

**Rationale**: honest demonstration (difference due exclusively to the
actions), reproducibility, alignment with the project thesis.

**Rejected alternative**: comparison with hard-coded values — not
credible.

**Refined by ADR #58 (workstream E8, 2026-06-04)**: the "same inputs"
framing is refined into a frozen-baseline counterfactual — the exogenous
parameters (climate, MAEC, PSE) are shared, but the four farmer decision
levers are frozen at their launch value.

---

### 10. 3-tier KPI hierarchy

**Context**: information density to organize without overwhelming the user.

**Decision**: 5 Hero KPIs (hedge density, composite biodiversity, water
table, integrated profitability, tech delta), 3 Tier B panels
(Biodiversity, Climate & resources, Economy), Tier C popovers on sensor
click.

**Rationale**: progressive information structure, quick reading possible,
depth available on demand.

**Rejected alternative**: flat exhaustive table — illegible.

---

### 11. Temporality: continuous simulation, x1/x10/skip, no day/night cycle

**Context**: choose a time model consistent with the observed phenomena.

**Decision**: continuous simulation, play/pause, x1 and x10 speeds, skip
to end beyond that. Pause maintains scene animations. No day/night
cycle. Seasons handled via shaders driven by the simulated weather, not
by calendar.

**Rationale**: observed phenomena (hedge growth, water table dynamics) at
a multi-year scale; day/night cycle out of scope and pointless.

**Rejected alternative**: discrete monthly tick — loss of granularity on
fast events.

---

### 12. Preset modification: interpolated 7-14 day transition

**Context**: avoid abrupt visual jumps when the user modifies a preset.

**Decision**: interpolated transition over 7-14 simulated days via
`TransitioningParameter<T>`.

**Rationale**: physical credibility (ecosystem parameters do not jump),
visual comfort.

**Rejected alternative**: immediate application — not very credible and
visually abrupt.

---

### 13. Sensor primacy: no visual driven by the calendar

**Context**: temptation to script effects (autumn leaves, snow) to set
the mood.

**Decision**: no visual element driven by the calendar. Everything derives
from a measurement or a model variable, traceable to a sensor or a
computation.

**Rationale**: this is what distinguishes a digital twin from a video game.
Guarantee of the demonstrator's honesty.

**Rejected alternative**: scripted decorative effects — loss of project
credibility.

---

### 14. Dual-hat user contract: Scenario + Decisions

**Context**: clarify what the user does.

**Decision**: **Scenario** hat (preset sliders, permanent) + **Management
decisions** hat (recommendations to arbitrate, appears on detected
events).

**Rationale**: cleanly separates context setting (passive) and the act of
management (active). Pedagogically clear.

**Rejected alternative**: everything in a single panel — conflates
parameterization and action.

---

### 15. Decision module: rich implementation

**Context**: level of ambition for the decision engine.

**Decision**: rich implementation, with uncertainties (distributions),
multiple horizons (short / medium / long term), contrast between user
choice and optimized choice.

**Rationale**: strong portfolio signal, demonstrates real decision
modeling in an uncertain environment.

**Rejected alternative**: simple if/then rules — mundane, not very
distinctive.

---

### 16. Camera: strictly fixed plan

**Context**: temptation to add parallax or a slight zoom.

**Decision**: strictly fixed plan, no parallax, no zoom.

**Rationale**: consistent with a dashboard format, simplifies production,
maximum legibility of sensor positions.

**Rejected alternative**: light parallax — marginal visual gain, cost in
sprite organization complexity.

---

### 17. Platform: desktop-only assumed

**Context**: whether or not to target mobile.

**Decision**: desktop only. No mobile responsive, no touch. Warning banner
if window < 1280 px.

**Rationale**: information density incompatible with mobile, sustainable
scope, portfolio target (recruiters on desktop).

**Rejected alternative**: mobile responsive — triple production cost with
no portfolio gain.

---

### 18. Scene ↔ data link: synchronized minimap/scene hover

**Context**: how to link the sensors visible in the scene to their
representation on the minimap.

**Decision**: synchronized minimap ↔ scene hover. No click from the scene
(to preserve immersive reading).

**Rationale**: legible and non-intrusive interaction; the minimap remains
the active entry point.

**Rejected alternative**: direct click on a scene sprite — interactive
noise, ambiguity with fauna animations.

---

### 19. Onboarding: contextual tooltips, no text intro

**Context**: how to explain the interface without an intrusive intro.

**Decision**: contextual tooltips in Garamond italic on hover, no text
intro at launch. Extremely explicit panel names.

**Rationale**: instant startup, exploration guided by hover, no blocking
modal.

**Rejected alternative**: step-by-step tutorial — high production cost,
intrusive for the portfolio audience.

---

### 20. 5-layer architecture

**Context**: code structure for a testable and maintainable Unity project.

**Decision**: 5 layers (SimulationCore / Sensors / Decision /
Indicators / Presentation). Asmdef per layer, strict references toward
lower layers only.

**Rationale**: testability of Layer 1 in pure C#, clean Unity / domain
separation, strong portfolio signal on software architecture.

**Rejected alternative**: monolithic MonoBehaviour architecture —
untestable, weak portfolio signal.

---

### 21. Communication pattern: observable ScriptableObjects + EventBus

**Context**: choose a Unity pattern for inter-layer communication.

**Decision**: observable ScriptableObjects (event `OnChanged`) for
indicators and persistent state; static EventBus for one-off events
(chalara detected, drought triggered, etc.).

**Rationale**: strong decoupling, inspectable in the editor, testable,
recognizable Unity idiom.

**Rejected alternative**: dependency injection (Zenject/VContainer) —
over-engineering for this scope.

---

### 22. Tick rate: 1 tick = 1 simulated day

**Context**: temporal granularity of the simulation.

**Decision**: 1 tick = 1 simulated day.

**Rationale**: trade-off between granularity (sufficient for daily events:
rain, sensor polling) and computational cost.

**Rejected alternative**: hourly tick — high cost with no gain for the
observed phenomena.

---

### 23. Deterministic seed with hash-derived sub-seeds

**Context**: guarantee reproducibility and shadow-simulation consistency.

**Decision**: master seed at startup, hash-derived sub-seeds for each
subsystem (weather, fauna, sensors, events).

**Rationale**: total reproducibility, isolation of randomness sources,
required for the real run / shadow run comparison.

**Rejected alternative**: a single global `Random` — impossible to
compare real and shadow run.

---

### 24. Shadow simulation: ISimulationRun interface, two instances

**Context**: technical implementation of the with/without tech comparison.

**Decision**: `ISimulationRun` interface, two instances with an
`applyTechActions` flag (true / false). Same seeds, same inputs.

**Rationale**: clean implementation, divergence guaranteed only by the
tech actions.

**Rejected alternative**: logic duplication — fragile, source of bugs.

**Superseded by ADR #58 (workstream E8, 2026-06-04)**: `ISimulationRun` /
`applyTechActions` were never built; the shadow uses a second concrete
`SimulationEngine` + a frozen-baseline `ScenarioContext`
(`TickWithoutAdvancingScenario`).

---

### 25. A single Unity scene (Main), 7 roots prefixed `_`

**Context**: organization of the Unity hierarchy.

**Decision**: single `Main` scene, 7 roots prefixed `_` (`_Bootstrap`,
`_Camera`, `_Scene_Visual`, `_Scene_Overlays`, `_UI_Canvas`, `_Audio`,
`_Debug`).

**Rationale**: simplicity, legible hierarchy, visual isolation of domains
in the editor.

**Rejected alternative**: additive multi-scene — over-engineering for this
scope.

---

### 26. Persistence: minimal PlayerPrefs

**Context**: what to save between sessions.

**Decision**: minimal PlayerPrefs — last preset configuration and chosen
speed. Nothing else.

**Rationale**: portfolio demonstrator, no user profile, no session save.

**Rejected alternative**: JSON session save — out of scope.

---

### 27. Logging: SimLogger 3 levels, no direct Debug.Log

**Context**: control log noise and WebGL runtime cost.

**Decision**: `SimLogger` with 3 levels (`DebugLog`, `SimulationLog`,
`UserActionLog`). No direct `Debug.Log` in application code.

**Rationale**: centralized filtering, disable-able in the build, portfolio
signal on instrumentation rigor.

**Rejected alternative**: `Debug.Log` everywhere — noise, runtime cost,
uncontrollable.

---

### 28. Audio: none

**Context**: whether to integrate sound.

**Decision**: no audio. No music, no sound effects, no ambient sound, no
UI sound feedback.

**Rationale**: the project is a silent observation station; avoid the
audio production cost; avoid WebGL audio pitfalls.

**Rejected alternative**: light ambient sound — production cost + WebGL
risks (autoplay policies) with no portfolio gain.

---

### 29. Asset pipeline: Nanobanana + ip-adapter + Python post-processing

**Context**: produce 15 unique style-consistent sprites.

**Decision**: Nanobanana with ip-adapter style reference (stylistic
reference image generated first), Python post-processing (palette
quantization, alpha cleanup, normalization).

**Rationale**: inter-sprite stylistic consistency, palette control, rapid
iteration.

**Rejected alternative**: buying an asset pack — loss of visual identity;
manual drawing — out of time scope.

---

### 30. Portfolio strategy Position C: AI usage soberly acknowledged

**Context**: how to position the use of AI tools in a portfolio.

**Decision**: usage soberly acknowledged in the README ("Method" section),
distinguishing what is AI-assisted (code, sprites) from what is human
decision (architecture, scientific calibration, design).

**Rationale**: professional honesty, signal of maturity, no concealment
or over-valuation.

**Rejected alternative**: not mentioning it — dishonest and easily
detectable.

---

### 31. README in English

**Context**: language of the README.

**Decision**: English.

**Rationale**: international portfolio audience (recruiters, github
trending, English-speaking teams).

**Rejected alternative**: French — limits the portfolio audience.

---

### 32. No public mention of the completion time

**Context**: whether to indicate "completed in X weeks" in the portfolio.

**Decision**: no public mention of the completion time.

**Rationale**: the portfolio value is in the result, not the time; time is
misleading (AI-assisted vs solo) and invites off-topic comparisons.

**Rejected alternative**: explicit mention — biases the reading.

---

### 33. Git workflow: Claude Code executes (revised)

**Context**: who executes the Git commands.

**Decision (revised on 2026-04-25)**: Claude Code itself executes
`git add`, `git commit`, `git push` in Conventional Commits format, at
appropriate moments. The user retains a permanent power of intervention
(stop, amend, revert, no-push). Risky operations (force push,
`reset --hard`, rewriting pushed history) still require explicit
validation.

**Initial decision (rejected on 2026-04-25)**: the user executes all Git
commands, Claude Code only proposes the messages. Observation in use: high
conversational friction, each milestone required a manual copy-paste.

**Rationale**: fluidity of the production session. The history stays clean
as long as the messages remain rigorous and the commit moments are
well chosen. The permanent power of intervention is enough to catch any
drift.

**Rejected alternative**: reverting to the initial decision on a
case-by-case basis — inconsistent and needlessly costly.

---

### 34. Roadmap in 10 vertical steps with a demonstrable deliverable

**Context**: breaking down the project to drive production.

**Decision**: 10 vertical steps, each with a demonstrable deliverable
(end-to-end slice, not an isolated horizontal layer).

**Rationale**: allows clean cutting at any step, each milestone is a
"showable version", motivating.

**Rejected alternative**: horizontal breakdown by layer — risk of
delivering 80% of layers with no functional demo.

---

### 36. Data-driven scene composition via ScriptableObject

**Context**: at Step 4, choice between composing the scene by hand in the
Unity editor (drag & drop of sprites) or generating it from a
ScriptableObject read at boot.

**Decision**: data-driven composition. `SceneCompositionDefinition`
(ScriptableObject) lists the `ScenicElement`s (sprite, position, scale,
sorting layer, order). `SceneAssembler` (MonoBehaviour) instantiates
everything under `_Scene_Visual` at Awake.

**Rationale**: strong portfolio signal (data/presentation separation,
reproducibility), enables later composition variants
(summer/winter/drought preset) without touching the scene, aligned with
the digital twin thesis (the scene is a reading of data, not a staged
scene). Additional cost ~2× the code, deemed reasonable for a dozen
scenery elements.

**Rejected alternative**: manual composition in the Unity scene —
faster but with no architectural value, and forces modifying the scene
for each variation.

---

### 37. Shaders: Shader Graph for all runtime shaders

**Context**: choice between pure HLSL (`.shader`) and Shader Graph
(`.shadergraph`) for the project's shaders (sky, meadow, hedges, pond).

**Decision**: Shader Graph for all runtime shaders
(`SG_Sky`, `SG_Hedgerow`, `SG_Pond`, `SG_Meadow` upcoming).

**Rationale**: live preview in the editor (visual iteration 10× faster
when the effect is not trivial), maintainability by a non-graphics
specialist over the portfolio's lifetime, absorption of the
version-specific URP 2D plumbing. For the sky alone the argument is
marginal, but the uniformity of the shader pipeline is worth more than
the local optimum.

**Operational consequence**: Claude Code scaffolds the Shader Graphs by
specifying the contract (exposed property names, graph structure). The
user wires the nodes in the Unity editor from the step-by-step
instructions — a `.shadergraph` file being auto-generated YAML with
GUIDs, its out-of-editor authoring is not reliable.

**Rejected alternative**: pure HLSL — negligible gain on simple shaders,
loss on complex shaders.

---

### 38. Sorting layers of the 2D scene

**Context**: rendering order of the sprites in the 2D scene.

**Decision**: 7 sorting layers declared in `ProjectSettings/TagManager.asset`,
from back to front: `Sky`, `Background`, `Midground`, `Foreground`,
`Sensors`, `Fauna`, `FX`. The `Default` layer is kept for non-visual
objects.

**Rationale**: direct alignment with the scene semantics
(Charles Harper / A Short Hike categories), eliminates intra-category Z
order conflicts, simplifies authoring the `ScenicElement`s in the
`SceneCompositionDefinition`.

**Rejected alternative**: a single `Default` layer with fine management
via `sortingOrder` int — fragile and illegible.

---

### 35. No audio, no mobile, no intrusive modal

**Context**: elements to explicitly exclude from scope.

**Decision**: no audio (cf #28), no mobile support (cf #17), no intrusive
modal (intro, tutorial, blocking dialog).

**Rationale**: focus, sustainable scope, consistency with a silent
observation station.

**Rejected alternative**: "we'll see later" — leads to scope creep.

---

### 39. Order of the Hero KPIs in the hero strip (cause → effect pyramid)

**Context**: 5 Hero KPIs are planned in the dashboard
(`HedgerowDensity`, `WaterTable`, `BiodiversityComposite`,
`IntegratedProfitability`, `TechDelta`). The display order from left to
right tells the reader a story.

**Decision**: adopted order `Hedges → Water table → Biodiversity →
Profitability → Tech delta`. Physical substrate (hedges, water) on the
left, ecological integrator in the center, economic valuation on the
right, meta trade-off on the far right.

**Rationale**: pedagogical reading of an agro-ecological digital twin.
The causal chain is read from concrete to meta: landscape structure →
physical resource → ecosystem effect → economic effect →
"does the tech help?". Consistent with the project thesis (honest test
of the eco/eco convergence, cf §1 CLAUDE.md).

**Rejected alternatives**:
- *Honest ones on the left, stubs on the right* (Hedges / Water table /
  Profitability / Biodiv / Tech delta): arbitrarily separates biodiv and
  water table, which are conceptually linked.
- *By weight in the narrative* (Tech delta first): displaying in pole
  position a KPI that is worth 0 until Step 8 is a bad visual signal for
  the portfolio.

---

### 40. Rejection of stub Hero KPIs — defer until state variables exist

**Context**: at sub-step 6a, 3 of the 5 planned Hero KPIs
(`Biodiversity`, `Profitability`, `TechDelta`) have no corresponding
state variable in `EcosystemModel`. Initial temptation: implement them
as formulas derived from the 2 existing variables
(`HedgerowDensity`, `WaterTableDepth`) to "wire the pattern".

**Decision**: rejection of derived stubs. The 3 indicators and their
`RC_*` containers are **not** created as long as the underlying variables
do not exist. At 6b the 3 corresponding cards will display a visual
« à venir » (coming soon) placeholder with a label indicating the step
where the KPI will be honestly wired.

**Rationale**: the sensor primacy principle (CLAUDE.md §9) requires that
every displayed value be traceable to a model variable. An arbitrary
formula `0.65 × hedgerowNorm + 0.35 × waterNorm` that we would call
"composite biodiversity" *is* invented data, even if it is
deterministic. A portfolio on the thesis "honest test of the eco/eco
convergence" cannot display figures for biodiversity, profitability and
tech delta that rest on nothing.

**Consequence on the roadmap**:
- `BiodiversityComposite` → arrives at Step 8 (fauna & shadow run: the
  addition of `FaunaPopulation` to the model unlocks an honest
  aggregate).
- `IntegratedProfitability` → arrives at Step 7 (economy: addition of
  `CropYield`, `InputCost`, `MaintenanceCost`).
- `TechDelta` → arrives at Step 8 (shadow run wired, the aggregate is
  computable on (real − shadow)). **Refined by ADR #59 (workstream E8)**:
  the KPI is a cumulative NET value in €/ha, not instantaneous.

**Rejected alternatives**:
- *Wired stubs but visually flagged*: a tempting compromise but we would
  still have displayed false figures. The "stub" badge on the card would
  have been a cover-up.
- *Extend EcosystemModel now*: inflates Step 6 by ~30-50% and encroaches
  on Steps 7-8 planned for this work.

---

### 41. Pond and meadow shaders in HLSL rather than Shader Graph (partial revision of #37)

**Context**: at sub-step 9α (deliverable #4 of Step 9), two new runtime
shaders had to be delivered: `SG_Pond` (pond driven by the water table)
and `SG_Meadow` (meadow driven by humidity). Decision #37 said Shader
Graph for all runtime shaders.

**Decision**: local deviation from #37 — `S_Pond.shader` and
`S_Meadow.shader` are written in pure HLSL (`.shader`). `SG_Sky` and
`SG_Hedgerow` remain in Shader Graph and are not re-generated.

**Rationale**:
- The two shaders in question are simple (a color lerp driven by a float
  `[0,1]`). The SG "live preview" benefit is marginal here.
- Authoring a `.shadergraph` by hand is impractical (1500 lines of YAML
  with internal GUIDs), and that is precisely what CLAUDE.md §2 asks
  Claude Code to do. An equivalent HLSL `.shader` is 60-80 legible lines,
  versionable, modifiable without opening Unity.
- Consequence for what follows: the binding layer consumes the same
  interface (`MaterialPropertyBlock` on a float), so switching later to a
  Shader Graph is non-blocking (backlog item).

**Operational consequence**:
- The user no longer creates the shader graph in Unity for the pond and
  the meadow; the `.shader` files are imported as is.
- If we want a more advanced effect later (ripples on the pond, floral
  variation on the meadow), we can either extend the `.shader` files in
  HLSL, or refactor toward a `.shadergraph` reusing the same property
  interface. Documented in `BACKLOG.md`.

**Rejected alternative**: hold #37 strictly and ask the user to manually
create the two Shader Graphs from scratch — slows down the delivery of
Step 9 for zero visual gain in the current format.

---

### 42. Hedgerow health proxy derived in Layer 4, not a state variable

**Context**: at sub-step 9β, we wanted to modulate the hedge sprites by a
`_HealthT` channel representing the "health" of the linear feature.
Initial temptation: add a `HedgerowHealth` property to `EcosystemModel`
with biophysical update rules (chalara, drought, seasonal recovery,
etc.).

**Decision**: `HedgerowHealth` is NOT a state variable. It is computed on
the fly by `HedgerowHealthIndicator` (Layer 4) by aggregating the current
density and the active events of the EventLog (recent chalara, recent
drought) within a 60-day sliding window.

**Rationale**:
- The sensor primacy principle (CLAUDE.md §9) does not require a visual to
  be derived from a dedicated state variable — it requires it to be
  derived from a traceable model measurement or computation. A
  deterministic aggregation of EventLog + existing state fulfills this
  contract.
- Adding a state variable forces artificial dynamics rules (recovery
  rate, cross-coupling) with no benefit for the decision engine: health
  is a reading, not a lever.
- Keeping the model surface minimal makes testing and picking the project
  back up to add better visual effects in the backlog easier.

**Operational consequence**:
- The hedge shader (`SG_Hedgerow`) must read `_HealthT` when it is
  extended — backlog entry "SG_Hedgerow healthT node". In the meantime,
  the binding silently pushes the value; Unity ignores properties not
  declared by the shader.
- If a finer analysis becomes necessary one day (cumulative dry seasons,
  fragmentation of the linear feature), we will be able to promote
  `HedgerowHealth` to a state variable without breaking the binding API.

**Rejected alternative**: a `HedgerowHealth` state variable updated by a
`HedgeHealthDynamicsRule` — oversized for the current need, weighs down
the model.

---

### 43. AutoAction `ReduceInputs` applies its effect directly on the real model, not via the shared scenario

**Superseded by ADR #58 (workstream E8, 2026-06-04)**: the premise of the
shared scenario no longer holds (frozen-baseline shadow); `ReduceInputs`
now lowers `ScenarioContext.InputIntensityFactor` (practice change,
transition §15). The +0.05 `FaunaPopulation` / −200 `InputCost` nudges and
the proposed `RealRunTechAdjustment` channel are abandoned.

**Context**: at sub-step 8c.3, the `ReduceInputs` auto-action (consumed by
the recommendation of the same name + the eponymous manual button) must
translate a farmer's trade-off "reduce inputs occasionally" into a
mechanical effect on the simulated state. The natural path would be to
**lower `ScenarioContext.InputIntensityFactor`**: it is the scenario
channel intended to model the intensity of farming practices, and all
downstream (CropYieldDynamicsRule, InputCostDynamicsRule,
FaunaDynamicsRule) already consumes it.

**Architectural tension**: the `ScenarioContext` is **shared by reference
between the real run and the shadow run**
(cf. `ShadowSimulationRunner` which passes the same instance to guarantee
non-divergence due to the scenario). If the auto-action modified
`InputIntensityFactor`, the shadow run would mechanically undergo the
same change, and the TechDelta KPI — defined as "profitability gap
between real and shadow" — would effectively cancel out. The shadow run
would then cease to be the "scenario without tech decisions" that the DT
thesis claims to measure.

**Decision**: `AutoActionPipeline.ApplyOne` for `ReduceInputs` does **not**
alter the `ScenarioContext`. It injects its effects directly on the state
variables of the real run's `EcosystemModel`:
- `+0.05 × ratio` on `FaunaPopulation` (one-off insect boost)
- `−200 × ratio €/ha/year` on `InputCost` (immediate savings from avoided
  inputs)

The `ratio` being the user magnitude divided by the reference value
(`ReduceInputsRecommendation.IntensityCutPerStep`). The shadow run,
which shares the scenario but has its own `EcosystemModel`, is not
touched → the divergence is captured by TechDelta.

**Rationale**:
- Preserving the shadow run's semantics as a "twin without tech
  decisions" is non-negotiable for the credibility of the central KPI of
  step 8.
- The intended effect (fauna boost + cost drop) is sourced: IPBES 2019
  (fauna rebound after pesticide cessation), CIVAM field crops
  (conventional input savings).
- The alternative "clone the ScenarioContext and lower
  `InputIntensityFactor` on the real copy only" breaks the shared
  scenario's uniqueness invariant documented in ARCHITECTURE.md and
  imposes signature drift across the whole stack.

**Operational consequence**:
- The effect on profitability goes through `InputCost` rather than through
  the scenario chain. This is an approximation: the true "intensity
  reduction" effect would also propagate via `CropYieldDynamicsRule`
  (slightly lowered yield) and via the recurring costs of subsequent
  years. Here the drop is a one-shot occasional change on the state
  variable.
- Assumed limitation: if the user stacks several `ReduceInputs`
  auto-actions, `InputCost` can go arbitrarily low (clamped at 0). The
  economic rule catches it on the subsequent ticks by pulling toward the
  scenario target, but the transient peak is a known artifact.
- Documented in the XML doc of `AutoActionPipeline.ApplyOne` and recalled
  in the class comment.

**Exit path (post-MVP)**: introduce an adjustment channel specific to the
real run, of the type `EcosystemModel.RealRunTechAdjustment` (structured
vector, e.g. `{ inputIntensityDelta, hedgeDensityDelta, … }`), that the
biophysical rules consult in addition to the shared scenario. The shadow
run ignores it. `ReduceInputs` then modifies a semantically clear delta
(`inputIntensityDelta -= 0.2`) that propagates cleanly via the existing
rules. Estimate: 0.5-1 day of refactor, to be arbitrated post-publication;
also covers BACKLOG item #9 (investment capital) which suffers from a
similar tension.

**Rejected alternatives**:
- *Modify the shared scenario's `InputIntensityFactor`*: breaks TechDelta
  (the shadow sees the same drop).
- *Clone `ScenarioContext` to give each run its own and lower the
  intensity only on the real clone*: violates the scenario uniqueness
  invariant (ARCHITECTURE.md §3 — a single `ScenarioContext` per session,
  single source of truth for the farmer/framework levers). Pollutes the
  signatures of the sensor → recommendation → outcome chain with a notion
  of "scenario context belonging to whom".
- *Defer `ReduceInputs` to the backlog until the `RealRunTechAdjustment`
  channel exists*: deprives Step 8 of one of the three honest recos that
  demonstrate the sensor → reco → impact chain, and thus of a quarter of
  its demonstrative value.

---

### 44. Recommendation arbitration semantics: « Valider » (Accept) / « Voir plus tard » (See later) / « Ignorer » (Ignore) + Superseded verdict

**Context**: at sub-step 10a, the audit identified two frictions on the
decision popup. (1) Clicking **Ignore** on a recurring reco was not
enough — the `EventDetector` rebuilt the same detection 30 days later and
the popup popped up again in a loop. (2) Conversely, **See later** on N
successive occurrences of the same type accumulated N entries in the
history, the « Recommandations en cours (12) » (Ongoing recommendations)
button quickly becoming noisy.

**Decision**: three user verbs, three clear semantics, a fourth system
verdict to bound the history.

**User verdicts (three popup buttons)**:

- **`Valider`** (Accept) → verdict `Accepted`. The auto-action is applied on the
  real model (not on the shadow). The reco type is **removed** from the
  session ignore set — the next occurrence of the same type will pop up
  again, because the user showed that they were actively engaging with
  this category of decision.
- **`Voir plus tard`** (See later) → verdict stays `Pending`. The reco is added to a
  `_skippedRecommendationIds` set (key: instance id) on the
  `DecisionPopupBinding` side which prevents its auto-popup for the
  session. The user can re-open it from the history button. A **new**
  instance of the same type (different event id) will not be affected —
  its own auto-popup will trigger normally.
- **`Ignorer`** (Ignore) → verdict `Rejected`. **The entire TYPE** of the reco is
  added to `_ignoredRecommendationTypes` for the session. Any new reco
  whose id starts with the same prefix (before the `#`) is silently
  skipped at auto-popup. It remains visible in the history for revisit,
  but no longer interrupts the simulation.

**System verdict (auto-marked in the journal)**:

- **`Superseded`** → automatically marked by `DecisionJournal.Append`
  when a **new** reco arrives and a `Pending` one of the same type is
  already in the journal. The old one becomes `Superseded`, the new one
  takes its place as the only `Pending` of this type. Audit preserved
  (the Superseded entries remain in `Entries`), but `PendingEntries` only
  exposes the latest → the history list is bounded to 1 Pending entry per
  type.

**Consequences**:

- At most **1 Pending per type** at a given moment, regardless of the run
  duration.
- The `Accepted` and `Rejected` are NEVER touched by supersession — the
  user arbitration trail is intact for a future `SessionReporter` (never
  built — BACKLOG #4).
- The set manipulations on the `DecisionPopupBinding` side are in-memory,
  lost at the end of the session — no PlayerPrefs persistence
  (CLAUDE.md §16). A new session starts with a blank list of
  ignored / deferred types.

**Rationale for the double layer (journal + binding)**:

The journal is the **model** authority (persistent verdicts for audit);
the binding sets are the **UX** layer (auto-popup skipping so as not to
be tiresome). The two are independent:
- You can ignore a reco via Ignore → the journal knows it is Rejected,
  the auto-popup skips it via the type set.
- You can revisit via « Examiner » (Examine) in the history → the popup appears even if
  the type is in the ignore set (explicit override by user action).
- You can re-Accept it → journal moves to Accepted, ignore set cleared for
  this type, the next event will pop up.

**Rejected alternatives**:
- *No supersession, we accept that the history grows*:
  « Recommandations en cours (47) » (Ongoing recommendations) button
  illegible after a month of continuous sim run. Also rejects the MVP
  spirit.
- *Mark the new reco directly `Rejected` on arrival if its type is in
  `_ignoredRecommendationTypes`*: breaks the user's ability to change
  their mind from the history (nothing useful to examine if everything is
  already Rejected). We prefer to keep the new one `Pending` and just
  block the auto-popup.
- *Supersession in `EventDetector` instead of the journal (do not re-emit
  the event if the type was recently suppressed)*: violates §9 (the
  detector must reflect what the sensors see, not the history of
  decisions). We suppress at the presentation stage, not at the
  measurement stage.

---

### 45. Locking of the MVP scope by the functional-completeness principle

**Context**: project in Step 10 finishing (sub-step 10b-perf). Internal
audit identifies several heterogeneous open workstreams (dormant chalara,
EddyTower visual with no reality, WeatherStation with no Reader, 3 empty
Tier B tabs, fauna in backlog, absent capital, scalar biodiv). Real risk
of scope creep or its inverse (delivering an MVP with a taste of
unfinished). Target portfolio audience: tech recruiters + M1 jury, who
have consistent but distinct requirements.

**Decision**: lock the MVP scope by the functional-completeness principle.
Priority audience = tech recruiters (Unity/WebGL/software architecture)
and M1 jury (scientific rigor). Budget = 15-20h/week over 3 months max,
target 150 h. Guiding principle: "nothing in the scene or the code gives
a taste of unfinished". Corollary: "complete or remove (never leave as
is)". Detailed in `CLAUDE.md` §17 and §18.

**Rationale**: an honest portfolio and an M1 jury require a coherent
end-to-end MVP, not an accumulation of partial features. Functional
completeness is what creates the "production-ready" effect sought by
recruiters and defensible scientifically by a jury.

**Operational consequence**: opens 5 closing workstreams (ADRs #46 to
#54) rolled out over steps E1-E7 of the new `ROADMAP.md`. Removal of the
pre-decided cutting strategy (cf ADR #56).

**Rejected alternative**: continue in "feature after feature with a
growing backlog" mode — result: MVP with a taste of unfinished,
defensible neither in recruitment nor at the defense.

---

### 46. Total purge of the chalara code

**Context**: chalara detection was disabled at sub-step 10b sensor polish
(the IR camera trap does not detect a fungus, semantically false). The
`HedgeChalaraEvent` and `PlantHedgesRecommendation` classes were kept
dormant pending a suitable sensor (cf old BACKLOG #16). At the reframing
audit: having a single isolated disease (chalara, on ash only) in a model
with no other pathogen (wheat rust, septoria, rapeseed sclerotinia, oak
processionary) gives the impression of a plant-health model sketched out
then abandoned.

**Decision**: total removal of the chalara-related code. No
reintroduction in the MVP.

Mechanical implications:

- Removal of `Assets/_Project/02_Sensors/Events/HedgeChalaraEvent.cs`.
- Removal of the chalara penalty branch in
  `HedgerowHealthIndicator.Compute()` + `ChalaraPenalty` constant.
- Removal of the `case HedgeChalaraEvent` branch in
  `RecommendationProvenance.SensorDisplayFor()`.
- 6 EditMode tests using `HedgeChalaraEvent` → rewritten by replacing the
  `hedge-chalara#NN` references with `drought-prolonged#NN` and
  `PlantHedgesRecommendation` with `IrrigationAdviceRecommendation` as a
  fixture (preserves coverage on supersession and dedup).
- `docs/BACKLOG.md`: items #14 and #16 removed, replaced by a "Complete
  plant-health framework" item conditional on a crop phenology model.

**Rationale**: consistent with the guiding principle §17 (CLAUDE.md).
Either we put back a plant-health ecosystem all at once (pathologies +
pests with suitable sensors), or nothing. The "dormant chalara alone"
compromise gives the taste of unfinished that the MVP explicitly refuses.

**Operational consequence**: workstream E1 of the new `ROADMAP.md`. The
stash `stash@{0}` contains partial chalara cleanup patches recoverable
via `git stash show -p stash@{0}`. Estimate 2-4 h (including test
rewrite).

**Rejected alternative**: reintroduce chalara with a suitable sensor
(NDVI drone, field survey) without the rest of the plant-health
ecosystem — reopens the isolated-disease problem.

---

### 47. Unified refactor of manual actions via the journal

**Context**: at sub-step 10a, 3 "occasional intervention" buttons
(PlantHedges, Irrigation, ReduceInputs) were wired via
`SimulationRunner.ApplyManualXxx()` which apply the effect directly on
the real model, without going through the `DecisionJournal`. Debatable
asymmetry: the auto recos go through journal + verdict + supersession,
the manual actions bypass entirely. Reframing audit friction: real run
traceability incomplete, the future `SessionReporter` (never built —
BACKLOG #4) would not see the manual actions.

**Decision**: all manual actions go through the `DecisionJournal` as a
"manual" auto-accepted `IRecommendation`. No more direct model bypass.

Mechanical implications:

- `SimulationRunner.ApplyManualXxx()` → create an `IRecommendation` with
  `DefaultVerdict = AutoAccepted` and add it to the journal via
  `DecisionJournal.Append()`.
- `AutoActionPipeline.Apply()` remains the only one to modify the model
  (no bypass).
- `Id` convention: `"manual-plant-hedges#<day>"`,
  `"manual-irrigation#<day>"`, `"manual-reduce-inputs#<day>"`.
- `TriggeredByEventId = null` convention. Adapt
  `RecommendationProvenance.Format()`: fallback « Action déclenchée par
  l'utilisateur le jour X » (Action triggered by the user on day X) if
  `TriggeredByEventId == null`.
- Supersession of repeated manual actions: **cumulative**. Since the
  manual action is `AutoAccepted` from creation (not `Pending`), it does
  not trigger the supersession of other entries of the same type.
  `PlantHedges` +30 m/ha then +30 m/ha → +60 m/ha total, 2 distinct
  journal entries.
- `PlantHedgesRecommendation` remains useful (manual side only — no
  longer emitted by `RecommendationEngine.TryProduceFor` since 10b).

**Rationale**: clean architectural discipline, total traceability of
player decisions, applicable supersession, more defensible for an M1
jury. Aligns the "auto" and "manual" semantics on the same channel.

**Operational consequence**: workstream E1 of the new `ROADMAP.md`.
Estimate 3-4 h (refactor + tests).

**Rejected alternative**: keep the current bypass — violates the single
traceability principle and complicates the future `SessionReporter`.

---

### 48. 1-pool soil carbon model + end-to-end EddyTower

**Context**: the EddyTower sprite (covariance tower) has been present in
the scene since Step 6c but with no corresponding state variable.
Practical violation of the sensor primacy principle (CLAUDE.md §9).
BACKLOG item #13 (soil carbon state variable) was awaiting its turn. At
the reframing audit: either we remove the sprite (loss of a major
scientific argument), or we wire it.

**Decision**: implement the 1-pool soil carbon model in the MVP. The
EddyTower sprite becomes a functional end-to-end sensor
(measurement → displayed indicator, with no event or reco — consistent
with §17 guiding principle "OR displayed indicator").

Mechanical implications:

- New state variable `SoilCarbonStock` (tC/ha) in `EcosystemModel`,
  default 50.
- New rule `SoilCarbonDynamicsRule` (Layer 01): 1-pool model
  `dC/dt = inputs − k·C`, `k = 1/40 yr⁻¹` (calibration cf
  `CALIBRATION.md`).
- 2 new levers in `ScenarioContext`:
  `CoverCropsCoveragePercent` (0-100%) and
  `ResidueRestitutionPercent` (0-100%), with sliders in the scenario
  panel.
- New `EddyTowerSensorReader` (Layer 02): measures daily net CO2 flux
  with Gaussian noise. RNG sub-stream `"eddy-tower"`.
- New `SoilCarbonIndicator` (Layer 04) + `RC_SoilCarbonStock`
  (Data/RuntimeContainers).
- Display in the Climate & Resources tab (cf ADR #54).
- EddyTower inspection panel (cf ADR #53): daily flux graph + cumulative
  stock.
- 4-5 EditMode tests.

**Rationale**: wiring EddyTower massively strengthens the scientific
defensibility (INRAE "4 per 1000" topic, voluntary CO2 markets,
Low-Carbon Label). A sensor sprite with no reality in the model is a
visible violation of the sensor primacy principle, anti-portfolio.

**Operational consequence**: workstream E3 of the new `ROADMAP.md`.
Sources: Solagro Afterres 2050, INRAE 4 per 1000, Efese ecosystem
services, BDAT. Estimate 10-14 h (including the EddyTower inspection
panel).

**Rejected alternative**: remove the EddyTower sprite — resolves the
visual/code consistency problem but loses a major scientific argument of
the DT.

---

### 49. Visible fauna — 4 species pooled with frame-swap animations

**Context**: without visible fauna, the `RC_BiodiversityComposite` index
remains an abstract number. The disappearance of species when biodiv
drops is the central pedagogical signal of the bocage subject. BACKLOG
items #1 + #2 deferred since Step 9. Sprite drafts already available in
`Assets/_Project/05_Presentation/Scene/Sprites/Fauna/`
(4 species × 3-4 frames partially present).

**Decision**: implement the pool of 4 visible species (heron, owl,
buzzard, swallow) with frame-swap animations, response curves on biodiv.

Mechanical implications:

- `FaunaSpeciesDefinition.cs`: ScriptableObject per species with sprites,
  appearance threshold, spawn position, animation pattern.
- `FaunaPool.cs` (Layer 05): object pooling without runtime Instantiate
  (CLAUDE.md §6 compliant).
- `FaunaIdleMotion.cs` (Layer 05): simple frame-swap animation
  (3-4 frame cycle).
- `FaunaPoolBinding.cs` (Layer 05): observes
  `RC_BiodiversityComposite` + `RC_FaunaFactor*` (cf ADR #51) →
  spawn/despawn species according to response curves.
- Conditional Crunch DXT5 on the new sprites (cf conditional decision in
  `docs/ROADMAP.md` workstream E7).
- No `_HealthT` modulation on fauna (BACKLOG item #3 permanently removed,
  out of MVP).

**Rationale**: visible fauna is the element that transforms an indicator
dashboard into a living digital twin. Without it, the pedagogical chain
"inputs ↑ → biodiv ↓ → fauna disappears" remains abstract. Sketched
sprites with no integration = taste of unfinished explicitly refused by
§17.

**Operational consequence**: workstream E4 of the new `ROADMAP.md`.
Sources: ZNIEFF Perche, ONF, PNR du Perche for the bestiary and
thresholds. Estimate 10-13 h (sprites already sketched, final corrections
the user's responsibility).

**Rejected alternative**: defer the fauna post-MVP — violates the guiding
principle §17.

---

### 50. Investment capital + profitability horizon

**Context**: `IntegratedProfitabilityIndicator` aggregates revenues −
operating costs + subsidies, with no notion of depreciable capital. The
`PlantHedges` action (manual via ADR #47) has no represented upfront cost
→ farmer trade-off skewed toward systematic acceptance. BACKLOG item #9
was awaiting its turn. For an M1 jury, this is the easy criticism: "your
economic model ignores capital, it is unusable in agricultural advice".

**Decision**: model investment capital (on PlantHedges only, the only
action with a real upfront cost) and compute the profitability horizon
via shadow vs real.

Mechanical implications:

- `InvestmentCost` field (€/ha) on `IRecommendation` (computed for
  `ManualPlantHedgesRecommendation`: planted density × price per linear
  meter, source Réseau Haies 3-10 €/m).
- Text « Coût upfront estimé : X €/ha » (Estimated upfront cost) displayed
  in the decision popup (manual).
- `TotalInvestment` accumulation in `DecisionJournal` (sum of the
  `InvestmentCost` of applied entries).
- New `InvestmentHorizonIndicator` (Layer 04): computes the years to
  recover the investment, based on the real vs shadow profitability
  divergence.
- Display: « Horizon rentabilité : X ans » (Profitability horizon: X years)
  line in the decision popup and the Economy tab. « Non encore atteint »
  (Not yet reached) if not reached in the simulation.
- For manual Irrigation and ReduceInputs: `InvestmentCost = 0` (occasional
  action, cost integrated in `InputCost`).

**Rationale**: the central thesis of the DT is "honest eco/eco
convergence". Without capital, planting is free, therefore trivial to
accept, and the thesis is skewed. The profitability horizon is the
decisive argument of a real farmer — an industry standard (Chamber of
Agriculture, MAEC reference framework).

**Operational consequence**: workstream E5 (grouped with ADR #51).
Sources: Réseau Haies de France, MAEC planting cost reference framework,
FranceAgriMer wheat/milk prices, Chamber of Agriculture. Estimate 6-8 h.

**Rejected alternative**: defer post-MVP — loses the anticipable
criticism of the M1 jury.

---

### 51. Enriched biodiv — exposure of 3 factors (minimal overhaul)

**Context**: `BiodiversityCompositeIndicator` aggregates 50% fauna +
30% hedge + 20% inverse water, self-justified weightings with no precise
citation. BACKLOG item #15 (biodiv overhaul) was awaiting its turn. MVP
compromise: adding a 4th factor "Landscape diversity" would require 2 new
scenario sliders (`GrasslandPercent`, `CropDiversityIndex`) → added
complexity.

**Decision**: limited overhaul — no 4th factor in the MVP. Individual
exposure of the 3 current factors (habitat, water, inputs) via distinct
`RC_*` for display in the Biodiv tab. Recalibration of the weightings.
Addition of weak effects sourced from daily weather (heatwave) and soil
carbon.

Mechanical implications:

- `FaunaDynamicsRule` (Layer 01) overhauled: 3 factors (habitat, water,
  inputs) explicitly computed, exposed via `RC_FaunaFactorHabitat`,
  `RC_FaunaFactorWater`, `RC_FaunaFactorInputs`.
- Addition of a weak daily-weather effect (heatwave) on fauna: penalty
  beyond a daily T° threshold (sourced Hallmann 2017).
- Addition of a weak soil-carbon effect on fauna: bonus if stock
  C > threshold (living soils = more macrofauna).
- Recalibration of the weightings of the `BiodiversityCompositeIndicator`
  based on the literature (Vigie-Nature, Hallmann 2017, MNHN 2024).
- 3 displayable lines in the Biodiv tab (cf ADR #54).

**Rationale**: reasonable compromise. 3 displayable lines, scientifically
defensible, without the added complexity of new scenario sliders that
would have required a rework of the scenario panel UI.

**Operational consequence**: workstream E5 (grouped with ADR #50).
Sources: INRAE Vigie-Nature, Constant et al. 1976 (Réseau Haies),
Hallmann et al. 2017 (Krefeld), MNHN 2024. Estimate 6-8 h. Part deferred
to BACKLOG (4th Landscape diversity factor).

**Rejected alternative**: complete overhaul with a 4th factor +
2 sliders — higher cost with no critical MVP gain.

---

### 52. Seasonality + WeatherStation complete chain

**Context**: `WeatherUpdateRule` draws each day around fixed annual
means (12 °C, 2 mm/day), with no seasonal cycle. Day 1 and day 180 have
the same weather distribution. BACKLOG item #12 was awaiting its turn.
Most visible scientific gap in the eyes of an agroecologist.
WeatherStation sprite present since 6c with no formal Reader. Reframing
audit: double problem (model + incomplete sensor chain) solvable at once.

**Decision**: implement the full Track J — seasonality with monthly
Météo-France data (Mortagne-au-Perche 61 station, 1991-2020 normals),
Level 3 stochastic model (ON/OFF Markov chain for rain + log-normal
intensity), WeatherStation as an end-to-end pure-measurement sensor.

Mechanical implications:

- `SeasonalWeatherDataAsset.cs` (Layer 01): ScriptableObject with 12 T°
  values + 12 precip values + monthly Markov parameters
  (p_wet, mu, sigma).
- `WeatherUpdateRule` overhaul: reads the current month + scenario
  anomalies + draws Bernoulli(p_wet[month]) then LogNormal(mu[month],
  sigma[month]) if rainy + Gaussian T° noise (σ = 2 °C). RNG sub-streams
  `"markov-rain"` and `"weather-noise"`.
- « Mois de démarrage » (Starting month) widget (Jan-Dec combo) in the
  « Conditions initiales » (Initial conditions) section.
- `WeatherStationReader` (Layer 02): pure T° + precip measurement with
  Gaussian noise. No event, no reco — pure reading (option a adopted).
- Free seasonal cascade: `WaterTableDynamicsRule`, `HedgerowGrowthRule`,
  `FaunaDynamicsRule` become seasonal via their inputs (notably water
  table).
- Extension of `CropYieldDynamicsRule` + `InputCostDynamicsRule` to daily
  weather (option a): addition of a term dependent on the real weather
  (WeatherStation heatwave → direct economic effect).
- « Normales climatiques mois courant + suivant » (Climate normals current
  + next month) panel integrated into the WeatherStation inspection panel
  (cf ADR #53).
- Seasonal crises (heatwave, flood) and seasonal visual effects (sky,
  meadow) in the BACKLOG, out of MVP.

**Rationale**: without seasonality, the DT is defensible in technical
demonstration but scientifically unassailable by an agroecologist.
WeatherStation with no formal Reader violates the sensor primacy
principle. Joint resolution = high portfolio value.

**Operational consequence**: workstream E2 of the new `ROADMAP.md`.
Sources: Météo-France 1991-2020 normals Mortagne-au-Perche (61) station,
INRAE BBCH scale, ARVALIS Eure-et-Loir. Estimate 16-22 h (16 h base +
3 h CropYield/InputCost extension + 6-10 h Markov level 3).

**Rejected alternative**: seasonality with annual means + noise only
(without Markov) — less scientifically defensible, the complexity gain of
Markov is modest for a high benefit in front of a jury.

---

### 53. Inspection panel of clickable sensors

**Context**: the 5 sensors are visible in the scene but only reveal their
measurements via the aggregated Hero or Tier B indicators. No way to
directly inspect a sensor, to see its measurement series, to understand
the uncertainty (acoustic fragile at low density for example).

**Decision**: the 5 sensors become clickable. An inspection panel opens on
click, with content specific to each sensor (graphs of historical
measurements vs references).

Content per sensor:

| Sensor | Panel content on click |
|---|---|
| Piezometer | Water table depth graph 365 d + 2 thresholds (3.5 m drought alert, 5 m critical) + "consecutive days > 3.5 m" counter. |
| WeatherStation | 2 superimposed graphs: daily T° vs monthly normal, daily precip vs monthly normal. Display of current and next month normals. |
| AcousticSensor | Measured abundance graph (noisy) vs true abundance (model). Visualizes the uncertainty — acoustic fragility pedagogy at low density. |
| CameraTrap | Same as AcousticSensor. Allows understanding the fusion via `FaunaSensorReader`. |
| EddyTower | Daily CO2 flux graph + cumulative C stock (cf ADR #48). |

Mechanical implications:

- Click detection on a 2D sprite: `Collider2D` + `IPointerClickHandler`
  via Unity EventSystem + `Physics2DRaycaster` on the camera.
- Sliding window 365 d storage in each `*SensorReader`
  (mutualized via `ISensorHistory<T>` interface, shared with ADR #54
  tabs).
- Reusable `SensorInspectorPanel.uxml` component (UXML + USS),
  reconfigures according to the clicked sensor.
- Custom graph component in `VisualElement` with
  `generateVisualContent` callback.
- Closing: click outside, Esc key, close button.
- New binding `SensorInspectorPanelBinding` (Layer 05).

**Rationale**: transforms the sensors from "instrumented scenery" into
"inspection interfaces", aligned with the DT's observation station
identity. Allows a portfolio visitor to understand measurement
uncertainty in 2 clicks, a signal of scientific maturity.

**Operational consequence**: workstream E6 (grouped with ADR #54).
Estimate 12-21 h (4-6 h generic system + 3-5 h custom graph + 5-10 h
content for the 5 sensors).

**Rejected alternative**: display the measurement series in a dedicated
tab — less direct, breaks the spatiality of the DT.

---

### 54. 3 Tier B tabs all filled

**Context**: the 3 Tier B panels (Biodiversity, Climate & Resources,
Economy) have been in place since Step 6b but largely filled with "coming
soon" placeholders. Visible friction: rich UI structure, poor content.

**Decision**: the 3 Tier B tabs are all filled with rich sub-indicators
using the existing + new variables (seasonality, soil carbon, visible
fauna, capital, 3-factor biodiv).

Detailed content per tab:

**Biodiversity**:

| Line | Source variable |
|---|---|
| Composite index | `BiodiversityCompositeIndicator` |
| Habitat component (hedges) | `RC_FaunaFactorHabitat` (new via ADR #51) |
| Water component | `RC_FaunaFactorWater` (new via ADR #51) |
| Inputs component | `RC_FaunaFactorInputs` (new via ADR #51) |
| Visible species count | derived from `FaunaPool` (new via ADR #49) |

**Climate & Resources**:

| Line | Source variable |
|---|---|
| Water table depth | `WaterTableDepth` (already) |
| Mean T° 365 d rolling | `CurrentWeather` history (new via ADR #52) |
| Cumulative precipitation 365 d rolling | `CurrentWeather` history (new via ADR #52) |
| Soil carbon stock | `SoilCarbonStock` (new via ADR #48) |
| Net CO2 flux | `EddyTowerSensorReader` history (new via ADR #48) |

**Economy**:

| Line | Source variable |
|---|---|
| Crop yield | `CropYield` (already) |
| Input cost | `InputCost` (already) |
| Hedge maintenance cost | `MaintenanceCost` (already) |
| PSE payment | computed (already) |
| CAP payment (BPS + redistributive + eco-scheme + hedge bonus) | constants (already) |
| Cumulative investment | `journal.TotalInvestment` (new via ADR #50) |
| Profitability horizon | `InvestmentHorizonIndicator` (new via ADR #50) |

Mechanical implications:

- New bindings: `OngletBiodivBinding`, `OngletClimatBinding`,
  `OngletEconomieBinding` (Layer 05).
- 365 d sliding windows for `CurrentWeather` history and `EddyTower` flux
  history mutualized with those of ADR #53.
- Existing USS / UXML of the tabs to enrich.

**Rationale**: with all the tracks activated (E2-E5), we have precisely
created the variables that fill these tabs. Removing them would waste the
benefit of the previous decisions. Aligned with the guiding principle §17
"every present tab must display useful info".

**Operational consequence**: workstream E6 (grouped with ADR #53).
Estimate 10-12 h.

**Rejected alternative**: fill partially with the existing variables only
— result: 3 tabs displaying 2-3 lines each, a taste of unfinished refused
by §17.

---

### 55. Uniform rationale pattern (concrete action + « Effet modélisé » / Modeled effect)

**Context**: 3 previous wording proposals for the recos had been rejected
because they evoked non-modeled effects (beneficials, secondary
windbreak, general resilience). The current `RecommendationPopupBinding`
displays heterogeneous rationales depending on the origin of the reco.

**Decision**: adopt a uniform rationale writing pattern for all
recommendations (manual AND auto). Format: short Title (verb + object) +
Rationale = concrete action sentence + a « Effet modélisé : ... » (Modeled
effect) line with figures on the variables actually touched. No flourish,
no non-modeled chimeras.

Exact wordings for manual actions (literal French UI strings displayed
in-app):

| Reco | Title | Rationale |
|---|---|---|
| `manual-plant-hedges` | Planter des linéaires de haies | Plantation d'essences sur bordures de parcelles. Effet modélisé : +X m/ha de densité de haies, +Y €/ha/an de coût d'entretien proportionnel. |
| `manual-irrigation` | Irrigation ponctuelle | Apport d'eau ciblé sur 30 jours. Effet modélisé : remontée temporaire de la nappe phréatique de X m (plancher 0,5 m). |
| `manual-reduce-inputs` | Baisser l'intensité d'intrants | Réduction des intrants chimiques sur 30 jours. Effet modélisé : +Y de population faune, −Z €/ha de coût d'intrants. |

X, Y, Z = values parameterized by the magnitude slider at the moment of
the click.

Standardization of the auto recos (option α adopted): apply the same
pattern to the 2 existing auto recos, adding a « Déclenché par : <event> »
(Triggered by) line in addition:

- `IrrigationAdviceRecommendation` (auto): Title « Irrigation ciblée +
  couvert anti-évaporation »; Rationale « Apport d'eau ciblé + couverts sur
  30 jours. Effet modélisé : ... Déclenché par : Sécheresse prolongée
  détectée par le piézomètre. »
- `ReduceInputsRecommendation` (auto): Title « Baisser l'intensité
  d'intrants »; Rationale « Réduction des intrants chimiques sur 30 jours.
  Effet modélisé : ... Déclenché par : Anomalie acoustique faune détectée
  par le capteur acoustique. »

**Rationale**: the « Effet modélisé : ... » (Modeled effect) line
explicitly indicates the limits of the model — a discipline we claim
everywhere. Uniform format = immediate reading by the visitor, and a
guardrail against non-modeled chimeras.

**Operational consequence**: workstream E1 (coupled with the manual
actions refactor ADR #47). Rewriting of labels. Estimate included in E1.

**Rejected alternative**: free-form rationales at the whim of the recos —
loses uniformity and risks mentioning non-modeled effects.

---

### 56. Removal of the pre-decided cutting strategy

**Context**: the historical §17 section of `CLAUDE.md` listed a cutting
order (medium decision → healthT removal → test reduction → sprite
reduction → do not cut architecture). Reframing audit: the scope is
locked by this session (cf ADR #45), the budget slack is comfortable
(~30-65 h on a 150 h target), the historical overruns were linked to
scope pivots (now forbidden by discipline §18 rule 2), not to bad
estimates.

**Decision**: the "final cutting strategy" §17 section of `CLAUDE.md` is
removed. No pre-decided cutting strategy. If we exceed 150 h, the user
arbitrates on a case-by-case basis in accordance with the guiding
principle.

**Rationale**: consistent with the "complete or remove" rule (§18 rule 8)
— we choose not to have this mechanic rather than having a half-done one.
Having a documented cutting strategy when we do not intend to use it
invites self-justification of shortcuts.

**Operational consequence**: §17 removed in `CLAUDE.md`, replaced by §17
MVP Scope + §18 Discipline. §18 In case of doubt renumbered to §19.

**Rejected alternative**: keep a "just in case" cutting strategy —
contradicts the locked scope and the guiding principle.

---

### 57. All sensors rendered as "online" — "pending" concept deferred

**Context**: `SensorPlacement_Default.asset` historically distinguished
`Online` and `Deferred` sensors (visually: green dot vs ochre dot in the
« Capteurs déployés » (Deployed sensors) list, plus a legend at the foot
of the list). State at
2026-06-02 (E6 delivery): the 5 sensors all have a complete end-to-end
chain — `PiezometerReader`, `WeatherStationReader`,
`EddyTowerSensorReader`, and the two channels
`AcousticSensorReader`/`CameraTrapSensorReader` exposed by
`FaunaSensorReader` each have a 365 d history and feed the inspection
panel (ADR #53). No sensor is any longer "pending" in the technical
sense.

But correcting the SO (moving the 3 sensors still marked `Deferred` to
`Online`) ran into a stubborn Unity cache: the corrected disk file was
not re-read, and even after an explicit reimport the UI list continued to
display gray. Forcing the value via the Unity Inspector worked on the
asset but not at runtime — disconnect not diagnosed in reasonable time.

**Decision**:

- `SensorListBinding.BuildRow` now ignores `meta.OnlineStatus` and
  unconditionally applies the `.sensor-status-dot--online` class.
- The online/deferred legend at the foot of `Dashboard.uxml` (block
  `.sensor-list-legend`) is removed — a single visual state does not
  deserve a legend.
- The `OnlineStatus` field remains present in `SensorPlacementDefinition`
  and `SensorMetadataTag` so as not to lose the data — when a backlog
  item "sensor broken / maintenance" reactivates the concept with a REAL
  narrative use case, the code returns to it by removing the hardcoded
  line and restoring the legend.

**Rationale**: aligned with the guiding principle §17 "every present
element must have an observable effect and an understandable narrative
interest". An ochre dot with no concrete use case (no failure scenario,
no simulated maintenance, no "faulty sensor" event) is parasitic info —
the portfolio user sees 3 gray dots and legitimately wonders "what is
not working on my end". Answer: nothing. So we remove the distinction
rather than explain a false problem.

**Operational consequence**: none on the E1-E7 roadmap. The
reintroduction of the concept is conditioned on a future backlog item
that would stage an intentionally offline sensor (failure, maintenance,
dead battery of a solar-powered sensor, etc.) — which would pedagogically
justify the visual distinction.

**Rejected alternative**: continue debugging the Unity cache and maintain
the distinction. Costly diagnosis (already burned ~30 min without
identifying the root cause), zero narrative gain as long as the concept
remains theoretical.

---

### 58. Shadow run = frozen-baseline counterfactual

**Context**: at the opening of workstream E8 (tech delta overhaul), the
shadow chain as documented by ADRs #9, #24 and #43 rested on two ideas
that no longer held at implementation. (1) #9 and #24 described a "same
seeds, same inputs" shadow run carried by an `ISimulationRun` interface
with two instances and an `applyTechActions` flag. (2) #43 assumed that
the `ScenarioContext` was shared by reference between the real run and
the shadow, which forbade `ReduceInputs` from touching
`InputIntensityFactor` (the shadow would have undergone the same drop,
canceling the KPI). Neither of these two constructions survived:
`ISimulationRun` and `applyTechActions` were never written, and the total
sharing of the scenario made a measurable practice change impossible.

**Decision**: the shadow run is a frozen-baseline counterfactual. It
shares with the real run the exogenous parameters (climate, MAEC, PSE)
**by reference**, but **freezes at their launch value** the four farmer
decision levers (`HedgeRemovalRate`, `InputIntensityFactor`,
`CoverCropsCoveragePercent`, `ResidueRestitutionPercent`) via
`ScenarioContext.CreateFrozenShadowFrom`. The shadow has its own
`EcosystemModel` and advances via `TickWithoutAdvancingScenario` (it does
not advance the scenario, which remains driven by the real run). The tech
value KPI measures exactly the real-vs-frozen-farmer gap: everything the
user changes after launch diverges from the frozen twin.

**Rationale**:
- A "same inputs" shadow shared by reference cannot serve as a
  counterfactual as soon as a decision modifies the scenario: it moves
  with the real run and the delta cancels out. Freezing the farmer levers
  alone, while sharing the climate and payment frameworks, cleanly
  isolates the contribution of the management decisions — this is the
  "twin without tech decisions" semantics that the DT thesis claims to
  measure (cf #43, tension now resolved).
- Sharing the exogenous parameters by reference guarantees that no
  divergence comes from the climate or the schedules: the non-divergence
  due to the exogenous scenario is structural, not to be recalibrated.
- Unlocks the practice change: `ReduceInputs` can go back to being a real
  slider on `InputIntensityFactor` (transition §15) without breaking the
  KPI, since the frozen baseline does not follow.

**Operational consequence**: replaces the "same inputs" framing of ADRs #9
and #24 and reverses the shared-scenario premise of ADR #43.
`ISimulationRun` / `applyTechActions` are recorded as never built
(ghosts). Evidence in the code: `ScenarioContext.CreateFrozenShadowFrom`,
`ShadowSimulationRunner` (second concrete `SimulationEngine` +
`TickWithoutAdvancingScenario`).

**Rejected alternative**: fully clone the `ScenarioContext` to give the
shadow an independent scenario — loses the sharing of the exogenous
parameters (the climate would diverge), reintroduces the scenario
uniqueness invariant discussed in #43, and blurs the delta semantics.

---

### 59. "Tech contribution" = cumulative NET value (integrated gross gain minus action investment), payback = day the NET crosses 0

**Context**: at the E8 overhaul, the "tech delta" Hero KPI had to honestly
quantify the contribution of instrumentation and decisions. The implicit
framing inherited from ADR #40 ("aggregate computable on (real −
shadow)") suggested an instantaneous profitability gap. But the effect of
an occasional action on the instantaneous gap peaks at the moment of the
action then decreases toward 0 as the system rebalances: an instantaneous
KPI would then display a contribution that "evaporates", which is false
from the standpoint of the value actually created.

**Decision**: the KPI integrates from day 0 the daily gap in integrated
profitability between the real run and the frozen-baseline shadow
(**gross** quantity, cumulative), then **subtracts the cumulative upfront
capital of the actions** (sensor costs excluded) to display the **NET**
value in €/ha. The profitability horizon ("payback") latches the **first
day the NET reaches equilibrium** (NET ≥ 0).

**Rationale**:
- Integrating capitalizes the value actually created: a transient peak
  that falls back to 0 still produced value over its duration, and the
  integral preserves it. A strategy is judged on its true horizon, not on
  a misleading snapshot.
- Subtracting the action investment gives an honest NET: a high gross gain
  obtained at the cost of heavy capital is not the same result as a modest
  gross gain for free. The payback (day the NET crosses 0) is the decisive
  argument on the farmer side.
- Excluding sensor costs: instrumentation is the DT's hypothesis (the
  "observe" line item), not a management action accounted for in the
  trade-off; we measure the contribution of decisions, sensors assumed in
  place.
- Supersedes the instantaneous framing suggested by ADR #40.

**Operational consequence**: evidence in the code —
`CumulativeTechValueIndicator` (integrated gross gain),
`InvestmentHorizonIndicator` (NET payback latch), `SimulationRunner`
(`net = gross − totalInvestment`).

**Rejected alternative**: display the instantaneous profitability gap —
spike then decrease toward 0, massively underestimates the value of a
strategy whose effect is transient but real, and makes the KPI illegible
over time.

---

### 60. Concave yield response (Mitscherlich) + fixed/variable input cost (70/30) ⇒ emergent profit optimum I\* ≈ 0.81

**Context**: the response of yield to input intensity was linear, and the
input cost was treated as entirely variable. Consequence: profit was
monotonic in intensity (more inputs = always more or always less profit
depending on the slopes), with no interior optimum. But the E9 economic
recommendations (notably "raise inputs toward the optimum") only make
sense if there is a maximum profit point toward which to orient the
farmer.

**Decision**: replace the linear yield-vs-intensity response with a
concave plateau curve (Mitscherlich type, **curvature 0.70**, plateau
beyond I = 1), and split the input cost into **70% structural fixed +
30% variable** (`VariableCostShare = 0.30`). The combination "decreasing
returns yield + variable share of the cost" makes an **interior profit
maximum near I ≈ 0.8** emerge (computed optimum I\* ≈ 0.81), the target
toward which the economic recommendations orient.

**Rationale**:
- A concave response is the correct agronomic form (law of diminishing
  returns: each additional input unit returns less). The plateau bounds
  the gain beyond the reference dose.
- A fixed cost share (structure, mechanization, land) that does not
  decrease with intensity is what creates the interior optimum: without
  it, profit would remain monotonic. The curvature/variable-share pair is
  what produces I\* ≈ 0.81.
- Gives a quantified and defensible anchor point to the economic
  counter-recommendations of ADR #61 ("raise toward I\*").

**Operational consequence**: sources and derivation of the 0.70 curvature,
of `VariableCostShare = 0.30` and of the I\* computation in
`CALIBRATION.md` section E8-E9. The I\* target is consumed by the
recommendation engine (ADR #61).

**Rejected alternative**: keep the linear response + all-variable cost —
no interior optimum, so the economic recommendations "raise/lower inputs
toward the target" would have no convergence point to aim for.

---

### 61. E9 recommendation system: 8 recos / 6 levers, state-aware dispatch, economic counter-recommendations, popup-vs-list surfacing by outcome classification

**Context**: the recommendation engine had 3 recos (irrigation, input
reduction, manual planting) on a small number of levers, all oriented
"more ecology". Three gaps for workstream E9: (1) no economic recovery
recommendation when profitability drops, (2) no trigger on low soil
carbon despite the 1-pool model (ADR #48), (3) undifferentiated surfacing
— every reco interrupted with a popup, without distinguishing a clear
gain from a value-laden compromise.

**Decision**: move from 3 to 8 recommendations on 6 levers (new:
`RaiseInputs`, `SowCoverCrops`, `RestoreResidue`, `ReduceHedgeRemoval`,
`IncreaseHedgeRemoval`). The engine performs a **state-aware dispatch**:
it selects the lever that has real room for maneuver in the current state
(and stays silent if none, consistent with §17), and emits **economic
counter-recommendations** (raise inputs toward I\* — cf ADR #60; thin out
unsubsidized over-dense hedges) on a new **`LowProfitabilityEvent`**
(threshold 50 €/ha — an indicator-threshold event, **not** a sensor
reading), alongside a new **`SoilCarbonLowEvent`** (threshold 45 tC/ha).
The economic counter-recommendations are **conditioned on a biodiversity
≥ 0.30** (we do not push to intensify when the ecosystem is already
critical). Each reco is classified by the sign of its projected long-term
deltas (profit / biodiversity) in `RecommendationSurfacing.Kind` ∈
{`WinWin`, `EconomicTradeoff`, `EcologicalTradeoff`, `LoseLose`}.
Surfacing:
- `WinWin` → **always** in popup.
- `EcologicalTradeoff` → popup **only** if biodiversity critical
  (< 0.30).
- `EconomicTradeoff` → stays in the **passive list**, with a « compromis »
  (compromise) badge; does not interrupt.
- `LoseLose` → not pushed.

**Rationale**:
- An engine that only knows how to recommend "more ecology" is not an
  honest decision-support tool: a farmer whose profitability is dropping
  needs economic levers. The counter-recommendations, gated on biodiv ≥
  0.30, balance the thesis without betraying ecology.
- The state-aware dispatch (lever with room) avoids recommending an action
  with no effect (e.g. reducing already-low inputs) and justifies silence
  when no lever has room (§17).
- Classifying by the sign of the projected deltas makes the surfacing
  **derived from the model**, not from a script: only a clear gain
  (win-win) or an ecological trade-off in a critical situation deserves to
  interrupt; any value-laden (economic) compromise stays passive and
  flagged « compromis » (compromise), leaving the arbitration to the user.
- `LowProfitabilityEvent` is explicitly an **indicator-threshold** event
  (profitability < 50 €/ha), not a sensor measurement: consistent with the
  sensor primacy principle (§9), it derives from a model computation
  traced to `IntegratedProfitability`.

**Operational consequence**: evidence in the code —
`RecommendationEngine`, `RecommendationSurfacing`, the 5 new recos,
`SoilCarbonLowEvent` / `LowProfitabilityEvent`. Surfacing table
(Kind × condition → popup/list) in `CALIBRATION.md`.

**Rejected alternative**: keep 3 all-ecological recos and a uniform popup
surfacing — unbalanced engine (no economic recovery), and intrusive
popups on compromises that the user should arbitrate themselves in the
list.

---

### 62. Model-derived decision: forward projection, farmer objective, emergent optimum

**Context**: the outcomes displayed under each recommendation (the
worst-expected-best profit / biodiversity ranges) were **frozen
coefficients** (old `OutcomeProjector`), independent of the current
state. Three consequences: (1) the projection could lie about the state
(under RCP4.5 climate stress, a reco displayed a gain that the model
contradicts), (2) the profit optimum was **hard-coded** (`I* ≈ 0.8`, cf
ADR #60), (3) the lever selection followed a **fixed priority** (cf ADR
#61). For a digital twin, the recommendations AND their outcomes must be
**derived from the coupled model**, not asserted.

**Decision**: overhaul the decision chain so that it computes itself on
the model.
- **`ModelOutcomeProjector`** (Layer 03): for a lever, simulates forward
  (real `SimulationEngine`, on an independent copy of the state) the run
  "with lever" against a baseline "without", same seed and same weather,
  and takes the real ΔKPI (profit, biodiversity). The worst/expected/best
  band is the **spread over 3 weather realizations** (favorable / median /
  unfavorable), not an arbitrary ×0.5 / ×1.25. The Layer 04 indicators are
  injected as delegates: Layer 03 does not depend on Layer 04.
- **`FarmerObjective`**: an internal objective function
  `U = w_eco · profit̂ + w_bio · Δbiodiv`, with **farmer weights**
  (dominant economy `w_eco = 0.80`; weak direct biodiversity
  `w_bio = 0.20`, but which enters strongly through the economy — the
  projected profit already embeds the windbreak effect of hedges, the soil
  fertility, the PSE/MAEC subsidies and the yield resilience). Internal
  weights (no new slider, §17), sourced from the agricultural decision
  literature (Edwards-Jones 2006; Reimer et al. 2012).

  > **Update (post-R7)**: the `w_eco/w_bio 0.80/0.20` weighting described
  > above has since been replaced — `FarmerObjective` computes a
  > **risk-adjusted margin** `U = E[Δmargin] − λ·(E[Δmargin] − Δmargin_worst)`
  > (λ=0.5), without a biodiversity weight (ecology is already monetized in
  > the margin). Up-to-date ref: `docs/refonte/08_MODELE.md` §9.1 +
  > `FarmerObjective.cs`. And the projection now samples **9 seeded
  > realizations** (cf ADR R7), not 3.
- **Selection by ΔU**: for each event, the engine builds the **feasible**
  levers (margin guardrails kept, §17), projects each, and keeps the one
  that best improves `U`.
- **Emergent optimum**: the hard-coded `0.8` disappears
  (`RaiseInputsRecommendation.ProfitOptimalIntensityFactor` removed). An
  economic counter-recommendation triggers **only if the projection shows
  a real profit gain** — beyond the optimum, raising inputs projects a loss
  and is discarded. The optimum thus recomputes itself if the calibration
  moves.
- **Surfacing derived from the true value**: `RecommendationSurfacing`
  classifies from the real `OutcomeDistribution` (sign → Kind logic
  unchanged). The popup/list bindings **memoize** the projection
  (forward sim = thousands of ticks, never on a per-frame path). A declined
  event is **marked considered** (`DecisionJournal.MarkEventConsidered`) so
  as never to be re-projected.

**Rationale**:
- This is what makes the thesis honest AND rigorous: with farmer weights
  (economy first), ecology is only recommended where the instrumentation
  reveals that it pays — the response **emerges from the coupled model**,
  imposed neither by the weights nor by coefficients.
- The derived optimum removes a magic value ("precise and unassailable,
  every approximation assumed"). The concave calibration + 70/30 cost of
  ADR #60 (which MAKES the optimum exist) remains; only its value is no
  longer written in hard code.
- Each projection serves a real decision (selection, gating, surfacing) —
  no decorative mechanic (§17).

**Operational consequence**: evidence in the code —
`ModelOutcomeProjector`, `FarmerObjective`, `RecommendationEngine`
(selection by ΔU + economic gating), `RecommendationSurfacing`,
`DecisionJournal.MarkEventConsidered`, bindings `DecisionPopupBinding` /
`DecisionPanelBinding`. Weights `w_eco` / `w_bio` + profit normalization
scale (150 €/ha) documented in `CALIBRATION.md`. 261 EditMode tests green
(headless dotnet runner, Layers 01-04).

**Supersession**: replaces the frozen-coefficient projector and the
fixed-priority dispatch of ADR #61 (the surfacing table Kind × condition →
popup/list remains valid, but the signs now come from the real
projection). ADR #60 remains valid for the yield/cost form that creates
the optimum; only the quantified anchor `I* ≈ 0.8` is no longer consumed
in hard code — the optimum emerges.

**Rejected alternative**:
- Keep the frozen coefficients (cheaper in computation) — but the
  projection lies about the state (RCP4.5 case), exactly the flaw a
  digital twin must avoid.
- Continuous optimization of the magnitude (search for the lever's optimal
  dose) — over-engineering: the user chooses the magnitude with the
  slider, the projection at default magnitude is enough to classify (§17).

---

## Backend overhaul (2026) — key decisions

Structuring decisions of the model rewrite (I1-I6) and the S5 cutover.

### R1. Overhaul in parallel `*.Refonte` namespaces, then cutover

**Context**: the pre-overhaul model (layers 01-04) had reached its limits (incomplete couplings, frozen-coefficient decision). Rewriting in place would have broken the app for weeks.

**Decision**: develop the new model under `*.Refonte` sub-namespaces, coexisting with the old one, then switch over (S5 cutover) by removing the entire old model once the overhaul is validated in Play Mode.

**Rationale**: the app stays launchable at each step; the headless harness validates layers 01-04 continuously; the switchover is a single atomic verifiable commit.

### R2. Recalibration of the yield's nitrogen response (Arvalis/COMIFER/INRAE)

**Context**: yield capped too low and the "zero nitrogen" floor collapsed under agronomic realism.

**Decision**: `Y_pot` 7.0 → 7.6 t/ha (achievable potential, Agreste wheat Eure-et-Loir); addition of a mineralization term for the active fraction `Mh` ≈ 40 kgN/ha/year that the ICBM 2-pools under-represented; organic nitrogen loss 0.8 → 0.6/year.

**Rationale**: N120 reference → ~5.5 t/ha stable (no transient), N=0 floor ~52% of the plateau, optimum ~N120-160 consistent with Arvalis, ~13% inter-annual CV. Locked by `NitrogenResponseCalibrationTests`. Cf `docs/refonte/08_MODELE.md`.

### R3. Framing "representative annual crop of a wheat/rapeseed rotation"

**Context**: a Norman bocage does not do monoculture; defending the rotation is more honest. But the model is mechanically mono-crop.

**Decision**: the annual crop represents a wheat/rapeseed rotation, **calibrated on wheat** (dominant crop, documented nitrogen curves); the current yield ~5.5 t/ha is the representative average.

**Rationale**: keeps citable parameters (one crop = one source) while assuming the rotation in the narration and via the Grassland lever.

### R4. Instantaneous lever transitions (MVP)

**Decision**: the levers apply immediately; the old 7-14 day smoothing (`TransitioningParameter`) is removed.

**Rationale**: MVP simplicity; the effect remains legible and the model remains deterministic.

### R5. Hero strip realigned on the spec set (08 §8), supersedes #39

**Context**: Layer 04 (`HeroIndicators`) already computed the spec set (Margin, Yield, Biodiversity, Soil carbon, Water reserve %RU), but the dashboard's Hero strip still displayed the old pre-overhaul set (hedge density, water table depth, biodiversity, profitability, tech contribution) locked by #39 — with tooltips citing removed formulas (biodiv "50% fauna + 30% hedges + 20% water table", profitability "maintenance costs… EcosystemModel fields").

**Decision**: align the strip on the **6 spec cards**, cause → effect order: Water reserve (%RU) → Soil carbon → Biodiversity → Yield → Margin → Tech contribution. The Hedges and Water table cards leave the strip (their RCs stay alive: hedges → hedge shader, water table → pond shader + Climate tab). Addition of `RC_CropYield` (the only spec KPI still not published) + 3 Hero bindings (`CropYieldLabelBinding`, `SoilCarbonLabelBinding`, `WaterReserveLabelBinding`); removal of the 2 Hero-only bindings that became dead (`HedgerowDensityLabelBinding`, `WaterTableLabelBinding`). The Margin card keeps the `profitability-value` Label to reuse `IntegratedProfitabilityLabelBinding` without rewiring.

**Rationale**: the 5 spec state KPIs are the flagship variables of the overhaul model (defensible at the defense); the old #39 order described a model that no longer exists. The Water reserve opens the cascade (θ crossroads), hence its position at the head of the strip. Supersedes **DECISIONS #39** (pre-overhaul Hero order).

### R6. V1 enrichments: 4th biodiv factor, visible hedge health, orphans completed, persistent recos

**Context**: post-publication, batch of finishing touches on the living model and the UI.

**Decisions**:
- **4th biodiversity factor "landscape"** (`BiodiversityRule.LandscapeFactor`): evenness of the crop/grassland mosaic + hedge mesh, distinct from habitat (rewards heterogeneity, not quantity — a monoculture, even of grassland, is not very diverse). Weights recomposed 0.35/0.20/0.30/0.15. Exposed as the 4th line of the Biodiv tab via `RC_FaunaFactorLandscape`. *(Benton et al. 2003; Efese)*
- **Visible hedge health** (`HedgeFloraRule.VisualVigor`): vigor [0,1] **without** the resilience floor that drives the density dynamics, dedicated to the shader tint (the hedge browns under drought / nitrogen excess). Deliberately decoupled so as **not** to touch the density dynamics.
- **2 orphan RCs completed** (§18.8): `RC_Nitrogen` (write-only) → nitrogen line of the Climate tab; `RC_HedgerowHealth` (writer-less) → written from `VisualVigor`, consumed by the hedge shader (fix of the `Spawn Root` that pointed at `_Scene_Visual` instead of `Composition`).
- **Persistent recos**: a reco stays pending until processed (Accept / Ignore) or the lever is satisfied, instead of expiring 45 d after its event — a passive reco became unplayable at fast speed. Inbox bounded to 1 per event type.

**Rationale**: complete rather than remove (§18.8), densify the biodiversity (jury defensibility), and make the sensor → visual / tab chains legible end-to-end.

### R7. "Negative tech contribution in RCP4.5" report: assumed transient + 2 fixes

**Context**: in RCP4.5, accepting the recos pushed the tech contribution (Hero
KPI) below zero. Headless investigation (throwaway probe, 8 seeds × 3 starting
months, seasonal climatology of Tourouvre): **this is not a reco computation
bug**. Each surfaced reco has a strictly positive projected expected Δmargin
(guard `Utility > 0` ⇒ `E[Δmargin] > 0`), and the 24 cases end strongly positive
(+970 to +2060 €/ha). The negative is a **transient of a cumulative KPI**
(`real_capital − ghost_capital − investments`): accepting a good adaptation
(cutting the nitrogen, switching to grassland) makes the cumulative dive for the
time it takes the freshly planted grassland to overtake the still-standing crop
at the start of the drought year, then it largely catches up with the frozen
ghost which keeps burning its inputs.

**Decisions**:
- **Transient left as is** (user choice): it is the real cost of a transition,
  and showing it is consistent with the honest model thesis (§1). No KPI
  smoothing, no UI note.
- **MAEC gated on the effective IFT** (`EconomyRule`, Layer 01): it unlocked on
  the raw phyto slider, so a 100% grassland farm (no crop to treat,
  `cultivated_share = 0`) lost the 90 €/ha if the slider stayed at its reference
  value. Now gated on `cultivated_share × intensity`: a permanent grassland
  sprays nothing and stays eligible. Unchanged for an all-crop farm (g=0).
  Welcome side effect: shortens the transient above.
- **Projection with 9 weather realizations** (`ModelOutcomeProjector`, Layer 03,
  was 3): the min over 3 draws was too noisy an estimator of the worst case →
  recos sometimes misclassified. 9 stabilizes the expectation and the downside,
  at the cost of ~3× the projection time per reco (only runs once per event).

**Rationale**: do not disguise honest behavior as a bug, but fix in passing an
unjustified MAEC ineligibility and make the recos' uncertainty band more
reliable. Headless tests 129 → 132 (2 MAEC + 1 projection), all green.
