# Carnet de câblage de la scène Unity

> **Mis à jour 2026-06-04 : réconciliation E8/E9 (voir entrées E8/E9 du
> Journal des modifications).**
>
> **Mis à jour 2026-06-11 (cutover S5) : la scène ne porte plus que des
> composants refonte.** Le runner est `RefonteSimulationRunner` ; l'ancien
> `SimulationRunner`, le `ShadowSimulationRunner`, `AutoActionApplier`,
> `ManualActionsBinding` et `SimulationTraceRecorder` ont été supprimés du code
> et retirés de la scène. Les entrées de Journal antérieures qui les mentionnent
> sont historiques.

Document vivant. **Tenu à jour à chaque sub-étape par Claude Code.** Tu y
trouves, pour chaque GameObject de la scène `Main`, les components qui le
portent et les références (drag-and-drop dans l'Inspector) qu'il faut
brancher.

Si tu te perds en Play Mode ou pendant une action manuelle, ouvre ce
fichier d'abord : il dit ce qui est censé exister et où.

> Convention : `→` signifie "dans la rubrique de l'Inspector, glisse
> l'objet de gauche dans le champ de droite".

---

## 1. Racines de la scène (7, préfixées `_`)

D'après CLAUDE.md §8 :

```
_Bootstrap
_Camera
_Scene_Visual
_Scene_Overlays
_UI_Canvas
_Audio          (vide, conservée pour cohérence — pas de son dans le projet)
_Debug
```

---

## 2. `_Bootstrap`

Le GameObject `_Bootstrap` n'a **aucun enfant** : tous les composants
ci-dessous sont posés **directement sur le GameObject `_Bootstrap`
lui-même**. Les références internes (`Shadow Runner`, `Real Runner`)
pointent donc d'un composant vers un autre composant du même GameObject.

| Composant sur le GameObject `_Bootstrap` | Composant | Références à brancher |
|---|---|---|
| `BootstrapEntryPoint` | `BootstrapEntryPoint` | Point d'entrée de boot (ordonnancement du démarrage). Aucune référence drag-and-drop hors de `_Bootstrap`. |
| `SimulationRunner` | `SimulationRunner` (Couche 5 Presentation) | RCs Hero : HedgerowDensity, WaterTable, IntegratedProfitability, BiodiversityComposite, TechDelta. RCs presentation channels (9α/9β) : `SoilMoisture Container` → `RC_SoilMoisture.asset`, `Hedgerow Health Container` → `RC_HedgerowHealth.asset`. `Shadow Runner` → le composant `ShadowSimulationRunner` ci-dessous (même GameObject). **`Seasonal Weather Asset` ✨ (chantier E2)** → `Assets/_Project/Data/Weather/SeasonalWeather_Mortagne.asset` (créer via `Create → Bocage → Weather → Seasonal Weather Data` ; si laissé null le runner tombe sur les defaults Mortagne-au-Perche hardcodés dans `SeasonalWeatherDataDefaults`). |
| `ShadowSimulationRunner` ✨ (sub-étape 8b) | `ShadowSimulationRunner` | `Real Runner` → le composant `SimulationRunner` ci-dessus (même GameObject `_Bootstrap`). |

---

## 3. `_UI_Canvas`

C'est le GameObject qui porte le **`UIDocument`** chargeant `Dashboard.uxml`
+ `PanelSettings_Dashboard`. Toutes les `*Binding.cs` de la couche
Presentation vivent sur ce même GameObject (elles ont toutes
`[RequireComponent(typeof(UIDocument))]`).

### Components attendus sur `_UI_Canvas`

| Component | Champs sérialisés à brancher |
|---|---|
| `UIDocument` | Source Asset → `Dashboard.uxml` ; Panel Settings → `PanelSettings_Dashboard.asset` |
| `HedgerowDensityLabelBinding` | (vérifier — voir source) |
| `WaterTableLabelBinding` | (vérifier — voir source) |
| `WaterTableDetailLabelBinding` | (vérifier — voir source) |
| `IntegratedProfitabilityLabelBinding` | `Runner` → `_Bootstrap/SimulationRunner` |
| **`BiodiversityLabelBinding` ✨ (sub-étape 8b)** | `Container` → asset `RC_BiodiversityComposite.asset` |
| **`TechDeltaLabelBinding` ✨ (sub-étape 8b)** | `Container` → asset `RC_TechDelta.asset`. **E8 :** lit `RC_TechDelta.NetEurosPerHa` = valeur **NETTE en €/ha** (avantage cumulé du run réel sur le run fantôme, MOINS l'investissement upfront cumulé des actions) ; **peut être négatif** quand la mise de capital dépasse les gains banqués. |
| **`DecisionPanelBinding` ✨ (sub-étape 8c.3, refactor history)** | `Runner` → `_Bootstrap/SimulationRunner`. `Recommendation Popup` → glisse le `DecisionPopupBinding` voisin. Gère le bouton « Recommandations en cours (X) » et la list popup historique (click ligne = ré-ouvre la popup reco). **E9 :** chaque ligne de la liste des recos en attente classée comme compromis (via `RecommendationSurfacing.IsTradeoff(rec)`, Couche 03) affiche un badge « **compromis** » — visible sur les recommandations économie-contre-écologie. |
| **`DecisionPopupBinding` ✨ (sub-étape 8c.3 post-livraison polish)** | `Runner` → `_Bootstrap/SimulationRunner`. Affiche un popup modal centré dès qu'une reco apparaît dans le journal. Met la sim en pause, slider magnitude + 3 boutons (Valider / Voir plus tard / Ignorer), reprend la sim quand la file (hors recos différées) est vide. **E9 : l'auto-ouverture du modal est désormais filtrée par `ShouldAutoSurface(rec)`** (wrapper interne qui délègue à `RecommendationSurfacing.ShouldAutoPopup(rec, biodiversity)`, Couche 03) — les recos de compromis (économie-contre-écologie, et écologie-contre-profit hors urgence biodiv critique) **n'ouvrent PAS le modal** : elles patientent dans la liste du `DecisionPanel`. Seuls les win-win et les urgences écologiques (biodiv sous le seuil critique) interrompent l'utilisateur. |
| **`AutoActionApplier` ✨ (sub-étape 8c.3)** | `Runner` → `_Bootstrap/SimulationRunner`. **Composant porté par `_UI_Canvas`** (pas par `_Bootstrap`). Subscribes à `TickCompleted` et applique les recos Accepted/AutoAccepted au real engine seul (le shadow n'est jamais touché → TechDelta bouge). |
| **`InitialConditionsBinding` ✨ (sub-étape 8c.4)** | `Runner` → `_Bootstrap/SimulationRunner`. Wire 3 sliders (`initial-hedgerow-density-slider`, `initial-water-table-depth-slider`, `initial-fauna-population-slider`) + bouton `initial-reset-button`. Sliders verrouillés quand `CurrentDay > 0`. |
| **`MonthSelectorBinding` ✨ (chantier E2)** | `Runner` → `_Bootstrap/SimulationRunner`. Câble le `DropdownField name="starting-month-dropdown"` (combo Jan-Déc) + `Label name="starting-month-hint"` placés en tête de la section « Conditions initiales du bocage ». Écrit la sélection dans `ScenarioContext.StartingMonth` immédiatement ; la `WeatherUpdateRule` snapshote la valeur au prochain `Rebuild` (changement mid-run sans effet sur le run courant). |
| **`ManualActionsBinding` ✨ (sub-étape 10a, refondu E8)** | `Runner` → `_Bootstrap/SimulationRunner`. Câble **2 sliders + 2 boutons** des « Interventions ponctuelles » du décision-panel : `manual-plant-hedges-*`, `manual-irrigation-*`. Chaque clic appelle `SimulationRunner.ApplyManualXxx`, applique l'effet directement au real model (pas au shadow → TechDelta capte la divergence), pas de journal. **E8 :** la baisse d'intrants (« reduce-inputs ») **n'est plus une action ponctuelle** — c'est désormais une pratique **soutenue** réglée via le slider quotidien d'intensité des intrants (section « Décisions quotidiennes »), plus un bouton-impulsion. Il ne reste donc que **deux actions manuelles** (planter des haies, irriguer). |
| `HedgerowShaderBinding` | `Density Container` → `RC_HedgerowDensity.asset`. `Health Container` → `RC_HedgerowHealth.asset` (sub-étape 9β). `Spawn Root` → racine `Composition` enfant de `_Scene_Visual`. Scanne les enfants commençant par `hedge_`. |
| **`PondShaderBinding` ✨ (sub-étape 9α)** | `Container` → `RC_WaterTableDepth.asset`. `Spawn Root` → même racine `Composition` que HedgerowShaderBinding. Préfixe scanné par défaut : `pond`. |
| **`MeadowShaderBinding` ✨ (sub-étape 9α)** | `Container` → `RC_SoilMoisture.asset`. `Spawn Root` → même racine `Composition`. Préfixe scanné par défaut : `grass_`. |
| `SensorListBinding` | (lit les `SensorMetadataTag` posés dans la scène) |
| `ViewportWarningBinding` | (auto, lit `Screen.width`) |
| `ScenarioControlsBinding` | `Runner` → `_Bootstrap/SimulationRunner` |
| **`ScenarioPresetsBinding` ✨ (sub-étape 7c.2)** | `Runner` → `_Bootstrap/SimulationRunner` <br> `Controls Binding` → le component `ScenarioControlsBinding` sur **ce même GameObject** <br> `Presets` → tableau des 4 assets `ScenarioPreset_*.asset` (voir §5) |
| **`SpeedControlsBinding` ✨ (sub-étape 7c.3)** | `Runner` → `_Bootstrap/SimulationRunner` <br> Les 7 noms d'éléments UXML (`speed-pause-button`, `speed-x1-button`, `speed-x5-button`, `speed-x10-button`, `speed-x20-button`, `speed-skip-end-button`, `speed-day-counter`) sont laissés à leur valeur par défaut — ils correspondent aux `name=""` posés dans `Dashboard.uxml`. |
| **`OngletBiodivBinding` ✨ (chantier E6)** | `Biodiv Composite` → `RC_BiodiversityComposite.asset`. `Habitat` → `RC_FaunaFactorHabitat.asset`. `Water` → `RC_FaunaFactorWater.asset`. `Inputs` → `RC_FaunaFactorInputs.asset`. `Fauna Pool` → `_Scene_Visual/Fauna` (GameObject portant `FaunaPool`, pour le comptage espèces visibles). Tous les noms de labels en défaut. |
| **`OngletClimatBinding` ✨ (chantier E6)** | `Runner` → `_Bootstrap/SimulationRunner`. `Soil Carbon` → `RC_SoilCarbonStock.asset`. Lit les historiques météo/eddy via `runner.WeatherStation` et `runner.EddyTower`. La ligne nappe phréatique reste pilotée par `WaterTableDetailLabelBinding` (rien à brancher en double). |
| **`OngletEconomieBinding` ✨ (chantier E6, refondu E8)** | `Runner` → `_Bootstrap/SimulationRunner`. `Total Investment` → `RC_TotalInvestment.asset`. `Investment Horizon` → `RC_InvestmentHorizon.asset`. PSE/PAC sont recalculés depuis les constantes publiques de `IntegratedProfitabilityIndicator` pour ne jamais diverger du Hero KPI. **E8 :** `RC_InvestmentHorizon` pilote une ligne « **horizon de rentabilité** » à **3 états**, conditionnée par `RC_InvestmentHorizon.IsReached` : « **X ans** » (rentabilité atteinte), « **Sans objet** » (aucun investissement réalisé), « **Non atteint** » (investissement présent mais break-even jamais franchi). |
| **`NiveauBModalsBinding` ✨ (chantier E6)** | Aucun champ à brancher. Auto-câble les 3 boutons trigger (`biodiv-open`, `climat-open`, `economy-open`) avec leurs overlays (`biodiv-modal-overlay`, etc.) et leurs boutons X (`biodiv-modal-close`, etc.). Fermeture via X, clic en dehors de la card, ou Échap. |
| **`SensorInspectorPanelBinding` ✨ (chantier E6 / ADR #53)** | `Runner` → `_Bootstrap/SimulationRunner`. Tous les autres champs (noms UXML overlay/close/title/chart hosts/footer) en défaut. S'abonne à `SensorClickedEventBus` ; au clic capteur (sprite scène ou ligne UI), reconfigure le panneau via une des 5 méthodes `ConfigureFor*` puis défère le show d'1 frame (évite la race avec `OnMouseDown` legacy du sprite — voir DECISIONS #53). Instancie 2 `SensorTimeSeriesChart` programmatiquement dans les hosts UXML. |

---

## 4. `_Camera`

| GameObject | Components |
|---|---|
| `MainCamera` | `Camera`, `OrthographicCameraSetup` |

---

## 4 bis. `_Debug`

Racine de diagnostic mandatée par `CLAUDE.md` §8.

| Composant sur le GameObject `_Debug` | Composant | Références à brancher |
|---|---|---|
| `SimulationTraceRecorder` *(désactivé par défaut)* | `SimulationTraceRecorder` (Couche 05 diagnostics) | — (s'auto-abonne au `TickCompleted` du `SimulationRunner`). Composant **désactivé par défaut** ; à activer ponctuellement pour tracer un run. |

---

## 5. Assets ScriptableObject de scénario (sub-étape 7c.2)

Localisation suggérée : `Assets/_Project/05_Presentation/Scenario/Presets/`

Crée le dossier s'il n'existe pas, puis : clic droit dans ce dossier →
`Create > Bocage > Scenario > Preset`. Renomme chaque asset selon la
colonne « Filename ». Les valeurs sont alignées sur les 4 scénarios
validés par `CalibrationScenarioValidationTests` (cf CALIBRATION.md).

| Filename | Id (lowercase, no spaces) | Display name | T° °C | Précip % | Hedge removal m/ha/an | Input × | MAEC % | PSE €/m/an | Horizon j |
|---|---|---|---|---|---|---|---|---|---|
| `ScenarioPreset_Reference.asset` | `reference` | Référence Perche | 0 | 0 | 0 | 1.0 | 0 | 0.00 | 365 |
| `ScenarioPreset_RCP45_2050.asset` | `rcp45` | Trajectoire RCP4.5 | 2 | -20 | 0 | 1.0 | 0 | 0.00 | 1825 |
| `ScenarioPreset_BocageBio.asset` | `bocage-bio` | Bocage vertueux MAEC | 0 | 0 | 0 | 0.5 | 100 | 1.00 | 1825 |
| `ScenarioPreset_Intensif.asset` | `intensif` | Intensif sans bocage | 0 | 0 | 5 | 2.0 | 0 | 0.00 | 1825 |

Description (champ texte, montrée en tooltip sur le bouton) :

- Référence Perche : *Baseline calibrée RICA Agreste 2024. Profit ≈ 335 €/ha/an.*
- Trajectoire RCP4.5 : *Horizon 2050, +2 °C et −20 % précipitations. Profit attendu négatif.*
- Bocage vertueux MAEC : *Bio extensif, 100 % MAEC, PSE maximal. Profit > 900 €/ha/an.*
- Intensif sans bocage : *Arrachage soutenu, intrants ×2, aucune aide environnementale.*

---

## 6. RuntimeContainers (ScriptableObjects observables)

Localisation : `Assets/_Project/Data/RuntimeContainers/`

| Asset | Producteur | Consommateurs |
|---|---|---|
| `RC_HedgerowDensity.asset` | `SimulationRunner` | `HedgerowDensityLabelBinding`, `HedgerowShaderBinding` |
| `RC_WaterTableDepth.asset` | `SimulationRunner` | `WaterTableLabelBinding`, `WaterTableDetailLabelBinding` |
| `RC_IntegratedProfitability.asset` | `SimulationRunner` | `IntegratedProfitabilityLabelBinding` |
| `RC_BiodiversityComposite.asset` ✨ | `SimulationRunner` | `BiodiversityLabelBinding` |
| `RC_TechDelta.asset` ✨ | `SimulationRunner` via `CumulativeTechValueIndicator` (E8) | `TechDeltaLabelBinding` |
| `RC_SoilMoisture.asset` ✨ (9α) | `SimulationRunner` | `MeadowShaderBinding` |
| `RC_HedgerowHealth.asset` ✨ (9β) | `SimulationRunner` | `HedgerowShaderBinding` (slot Health Container) |
| `RC_SoilCarbonStock.asset` ✨ (E3) | `SimulationRunner` | `OngletClimatBinding` (E6), `SensorInspectorPanelBinding` (E6, mode EddyTower) |
| `RC_FaunaFactorHabitat.asset` ✨ (E5) | `SimulationRunner` | `OngletBiodivBinding`, `FaunaPoolBinding` |
| `RC_FaunaFactorWater.asset` ✨ (E5) | `SimulationRunner` | `OngletBiodivBinding`, `FaunaPoolBinding` |
| `RC_FaunaFactorInputs.asset` ✨ (E5) | `SimulationRunner` | `OngletBiodivBinding`, `FaunaPoolBinding` |
| `RC_TotalInvestment.asset` ✨ (E5) | `SimulationRunner` | `OngletEconomieBinding` |
| `RC_InvestmentHorizon.asset` ✨ (E5, refondu E8) | `SimulationRunner` via `InvestmentHorizonIndicator` | `OngletEconomieBinding` |

> `CumulativeTechValueIndicator` et `InvestmentHorizonIndicator` sont des
> indicateurs **Couche 04 en C# pur**, instanciés par `SimulationRunner`
> (pas de GameObject ni de composant en scène) ; ils alimentent
> respectivement `RC_TechDelta` et `RC_InvestmentHorizon`.

---

## 7. Comment retrouver une référence cassée

Si en Play Mode un binding logge *"runner is null"* ou *"slider not found"* :

1. Ouvrir ce fichier, retrouver la ligne du binding concerné.
2. Vérifier que **tous les champs sérialisés** listés ici sont bien
   renseignés dans l'Inspector du GameObject correspondant.
3. Pour un slider/label introuvable : vérifier que le `name=""` dans
   `Dashboard.uxml` correspond bien à celui attendu dans le binding
   (champs `[SerializeField] private string ...Name`).

---

## Journal des modifications

- **2026-06-04** — Chantier E9 livré (système de recommandations).
  **Aucun nouveau GameObject ni composant scène** : tout vit en
  Couche 02/03, sans référence en scène. Réécriture du
  `RecommendationEngine` + nouveau `RecommendationSurfacing`
  (`Assets/_Project/03_Decision/`), 5 nouvelles classes de
  recommandation et 2 nouveaux events
  (`Assets/_Project/02_Sensors/Events/`). `RecommendationSurfacing`
  classe chaque reco (win-win / compromis économique / compromis
  écologique / lose-lose) et expose le gate `ShouldAutoPopup(rec,
  biodiversity)`. Côté présentation (bindings existants, pas de
  nouveau composant) : `DecisionPopupBinding` filtre désormais
  l'auto-ouverture via son wrapper `ShouldAutoSurface(rec)` — les
  recos de compromis patientent dans la liste au lieu d'interrompre ;
  `DecisionPanelBinding` ajoute un badge « **compromis** » sur les
  lignes de la liste classées trade-off (`IsTradeoff`).

- **2026-06-04** — Chantier E8 livré (refonte du delta tech).
  **Aucun nouveau GameObject ni composant scène.** Le champ de
  `RC_TechDelta` est renommé en `netEurosPerHa` (propriété
  `NetEurosPerHa`, ancien `deltaPercent`/`cumulativeEurosPerHa`
  conservés via `FormerlySerializedAs`) : valeur **NETTE €/ha** (gain
  cumulé réel vs fantôme moins l'investissement upfront cumulé), qui
  **peut devenir négative**. `RC_TechDelta` est désormais alimenté par
  l'indicateur Couche 04 pur `CumulativeTechValueIndicator`, et
  `RC_InvestmentHorizon` par `InvestmentHorizonIndicator` (tous deux
  instanciés par `SimulationRunner`, sans GameObject). Réécritures de
  libellés/bindings : `TechDeltaLabelBinding` (lit la valeur nette,
  peut être négative) et `OngletEconomieBinding` (ligne « horizon de
  rentabilité » à 3 états gated sur `RC_InvestmentHorizon.IsReached` :
  « X ans » / « Sans objet » / « Non atteint »). Le bouton
  « reduce-inputs » est **retiré** de `ManualActionsBinding` : la
  baisse d'intrants devient une pratique soutenue sur le slider
  quotidien d'intensité (il ne reste que 2 actions ponctuelles :
  planter des haies, irriguer).

- **2026-06-02** — Chantier E6 livré (panneau inspection capteurs +
  onglets Niveau B + force-online). Cf `ROADMAP.md` §8 pour le
  détail des 4 sous-étapes (B.1 readers, B.2 click infra, B.3 graphe
  custom, B.4 modale). **Actions utilisateur scène** :
  1. Sur `_UI_Canvas` : Add Component ×5 (`OngletBiodivBinding`,
     `OngletClimatBinding`, `OngletEconomieBinding`,
     `NiveauBModalsBinding`, `SensorInspectorPanelBinding`) — cf
     §3 ci-dessus pour les champs à brancher. `NiveauBModalsBinding`
     n'a aucun champ. `SensorInspectorPanelBinding` n'a que
     `Runner`. Les 3 `OngletXxxBinding` demandent les RC observables
     correspondants.
  2. **Aucune modification scène requise sur les sprites capteurs** :
     `SensorVisualPlacer` ajoute automatiquement `SensorClickHandler`
     au démarrage. **Aucun `Physics2DRaycaster` à ajouter**
     (`OnMouseDown` legacy suffit, même pattern que le hover existant).
  3. `_Bootstrap/SimulationRunner` expose 2 nouvelles propriétés
     publiques (`Piezometer`, `FaunaSensor`) lues par
     `SensorInspectorPanelBinding` — aucun champ Inspector
     supplémentaire requis.
  Aucun nouveau RC observable créé (les historiques 365 j vivent
  dans les readers Couche 02, pas dans des SO). UI : layout final
  du bas-dashboard = 3 boutons compacts Niveau B + modale au clic
  (cf ADR #57 sur la force-online des dots capteurs).

- **2026-05-29** — Chantier E5 livré (capital + biodiv 3 facteurs).
  5 nouveaux RC observables (`RC_TotalInvestment`,
  `RC_InvestmentHorizon`, `RC_FaunaFactorHabitat`,
  `RC_FaunaFactorWater`, `RC_FaunaFactorInputs`) — voir §
  « Chantier E5 » plus haut pour les 4 étapes de câblage Unity
  (création des 5 assets puis branchement sur les 5 nouveaux slots
  Inspector du `SimulationRunner`). `InvestmentHorizonIndicator`
  (Couche 04) instancié par le runner en Awake/Rebuild — pas de
  GameObject dédié. Dashboard.uxml/uss étendus : nouveau label
  `decision-popup-investment` dans le popup décision, masqué pour
  Irrigation/ReduceInputs et affiché live (« Coût upfront estimé :
  X €/ha ») pour PlantHedges.

- **2026-05-29** — Chantier E3 livré (carbone sol + EddyTower bout-en-bout).
  Nouveau RC observable `RC_SoilCarbonStock.asset` (Couche 04, slot
  `Soil Carbon Container` du `SimulationRunner`). `EddyTowerSensorReader`
  instancié par le runner (lockstep avec `FaunaSensorReader` /
  `WeatherStationReader`). Dashboard.uxml étendu : 2 sliders 0-100 %
  ajoutés à la section « Décisions quotidiennes »
  (`cover-crops-slider`, `residue-restitution-slider`) câblés via
  `ScenarioControlsBinding` (push vers `ScenarioContext.CoverCropsCoveragePercent`
  + `ResidueRestitutionPercent`). Action utilisateur Unity : créer
  `RC_SoilCarbonStock.asset` via `Create > Bocage > Data >
  RC_SoilCarbonStock` puis le glisser sur le nouveau slot du
  `SimulationRunner`.

- **2026-05-27** — Sub-étape 10b polish capteur livrée : nouveau
  composant `FaunaSensorReader` (Couche 2, pas de GameObject —
  instancié par `SimulationRunner` en Awake et reconstruit en
  lockstep dans `Rebuild`). Modifie la signature de
  `EventDetector.Detect(model, log, measuredFaunaPopulation)` —
  l'alerte fauna se base désormais sur la lecture bruitée, pas la
  vérité modèle. `HedgeChalaraEvent` retiré du détecteur en
  préparation du chantier E1 (purge totale chalara, ADR #46).

- **2026-05-26** — Sub-étape 10a livrée : ajout du composant
  `ManualActionsBinding` sur `_UI_Canvas` (interventions ponctuelles
  Plant / Irrigate / Reduce inputs déclenchables sans attendre un
  événement). Sémantique d'arbitrage popup formalisée dans
  DECISIONS #44 : nouvelle valeur `DecisionVerdict.Superseded` + set
  `_ignoredRecommendationTypes` côté `DecisionPopupBinding`.

- **2026-05-26** — Sub-étape 9β finalisée : `SG_hedgerow.shadergraph`
  expose la propriété `_HealthT`. Second Lerp inséré entre la sortie
  du Lerp densité et le Multiply texture, T = `1 - _HealthT` via un
  node One Minus.

- **2026-05-25** — Sub-étape 9α livrée : 2 nouveaux bindings
  (`PondShaderBinding`, `MeadowShaderBinding`) sur `_UI_Canvas`, 2
  nouveaux RCs observables (`RC_SoilMoisture`, `RC_HedgerowHealth`).
  Materials `M_Pond.mat` et `M_Meadow.mat` (shaders HLSL `S_Pond.shader`
  et `S_Meadow.shader`, cf DECISIONS #41) à affecter via le custom
  inspector de `SceneComposition_Default.asset` sur les éléments
  `pond` et `grass_border`. `HedgerowShaderBinding` étendu : nouveau
  slot `Health Container` à brancher sur `RC_HedgerowHealth.asset`.

- **2026-05-21** — Sub-étape 8c.4 livrée : panneau « Conditions
  initiales du bocage » dans le panneau gauche (`scenario-panel`,
  après Politiques publiques). 3 sliders (HedgerowDensity 0-200,
  WaterTableDepth 0.5-10, FaunaPopulation 0-1.5) + un bouton dont
  le texte est dynamique : « **Lancer la simulation** » au tout
  premier démarrage (day=0 et sim pas encore lancée), sinon
  « **Réinitialiser la simulation** ». Click applique les sliders
  via `SimulationRunner.Rebuild(...)` puis `StartTicking()` —
  expérience one-click. Édition des sliders verrouillée à
  `CurrentDay > 0`. Le shadow runner et `SpeedControlsBinding`
  s'abonnent à l'event `Rebuilt` pour resynchroniser leur état
  (modèle shadow recréé, bouton speed actif mis à jour).

- **2026-05-21** — Sub-étape 8c.3 livrée :
  `AutoActionPipeline` (pure C# Couche 3) + `AutoActionApplier`
  (MonoBehaviour Couche 5) appliquent les recos Accepted/AutoAccepted
  au real engine seul. `DecisionJournal.MarkApplied/IsApplied` pour
  garantir l'idempotence. Decision panel UI (à droite, au-dessus de la
  sensor list) liste les recos pending avec outcomes 30j/365j et
  boutons accept/reject. TechDelta KPI doit maintenant bouger quand
  l'utilisateur accepte une reco.
  + Polish post-livraison :
    (a) ordre des opérations corrigé dans `SimulationRunner.TickLoop` —
    `PublishIndicators` appelé APRÈS `TickCompleted` pour que le shadow
    soit à jour avant la lecture, fixant un drift d'un tick sur le KPI
    TechDelta ;
    (b) seuils `EventDetector` retunés pour fire sous RCP4.5 : hedge
    60→75 m/ha, drought 5→3.5 m, fauna 0.5→0.7 ;
    (c) layout complètement refondu en 2 zones conceptuelles + 1
    panneau flottant :
    **gauche = Cadre extérieur** (préréglages climat×politique +
    conditions naturelles subies + politiques publiques),
    **droite = Espace agriculteur** (décisions quotidiennes
    hedge/intrants + horizon + recommandations à arbitrer),
    **bas-droite floating = Capteurs déployés**. Les préréglages
    n'appliquent QUE les paramètres exogènes (climat + politiques +
    horizon) ; les sliders agriculteur ne sont jamais modifiés par un
    clic preset. `ScenarioPresetDefinition` allégé : suppression des
    champs `hedgeRemovalRate` et `inputIntensityFactor`. Les 4 presets
    renommés en grille climat × politique : Référence, Politique
    vertueuse, Trajectoire RCP4.5, RCP4.5 + Politique forte.

- **2026-05-21** — Sub-étape 8b livrée :
  `BiodiversityCompositeIndicator` (composite 50 % fauna + 30 % hedge +
  20 % water inverse), `TechDeltaIndicator` (% delta rentabilité real vs
  shadow), `ShadowSimulationRunner` (run parallèle avec le même seed et
  scenario partagé), 2 RCs observables + 2 LabelBindings. Hero cards
  Biodiversité et Delta tech débarrassées du tag `--deferred`. Les 5
  Hero KPIs sont maintenant tous honnêtes.

- **2026-05-21** — `OrthographicCameraSetup.viewportRect.y` passé de
  0.2222 à 0.15 pour décaler le rendu de scène vers le bas et laisser
  une respiration sous le hero-strip. Hauteur inchangée (pas de
  déformation horizontale). Réglé manuellement via l'Inspector puis
  gravé dans le défaut C# et `Main.unity`.

- **2026-05-21** — Création du document. Ajout de `ScenarioPresetsBinding`
  + 4 assets de preset (sub-étape 7c.2).
- **2026-05-21** — Sliders du panneau scénario passés en
  `show-input-field="true"` : chaque slider expose maintenant un petit
  champ numérique éditable à droite pour saisir une valeur précise (ex.
  taper `+2.3` au lieu de tenter de pointer la valeur au curseur). Drag
  reste possible pour ajuster grossièrement.
- **2026-05-21** — Le démarrage de la simulation est maintenant
  toujours en pause (`SpeedControlsBinding.Start()` force `StopTicking`
  après le `Start()` du runner). La clé PlayerPrefs
  `Bocage.Speed.LastSpeed` est toujours écrite à chaque clic d'un
  bouton play mais n'est plus relue au démarrage.
- **2026-05-21** — Ajout de `SpeedControlsBinding` et de la barre de
  vitesse top-centre (sub-étape 7c.3). 6 boutons (pause / ×1 / ×5 / ×10
  / ×20 / skip-to-end) + compteur de jours. Skip-to-end avance le
  moteur de `HorizonInDays` jours puis met en pause. La vitesse choisie
  est persistée en PlayerPrefs sous la clé `Bocage.Speed.LastSpeed`
  pour usage futur ; elle n'est PAS appliquée au démarrage — la
  simulation boote toujours en pause pour que l'utilisateur lance
  lui-même le run.

---

## Câblages prévus post-recadrage 2026-05-28 (chantiers E1-E7)

Cette section liste les câblages prévus par les chantiers E1-E7 de
`docs/ROADMAP.md`. À mettre à jour au fil des livraisons en
basculant les entrées dans la section principale ci-dessus.

### Chantier E1 — Cleanup chalara + refactor actions manuelles (livré 2026-05-29)

Pas de nouveau câblage scène. Le `ManualActionsBinding` existant sur
`_UI_Canvas` (sub-étape 10a) reste inchangé côté binding : il appelle
toujours `SimulationRunner.ApplyManualXxx()`. C'est l'implémentation
de ces méthodes qui a été refactorée pour journaliser via
`DecisionJournal` au lieu d'appliquer directement (ADR #47).
Aucun champ Inspector nouveau.

### Chantier E2 — Saisonnalité + WeatherStation (livré — voir sections 2 et 3)

> Les noms exacts livrés font foi dans les sections 2 et 3 ci-dessus. Le
> tableau ci-dessous est conservé pour mémoire, avec les noms corrigés.

**Extension `_Bootstrap/SimulationRunner`** :

| Composant | Champ sérialisé | Référence à brancher |
|---|---|---|
| `SimulationRunner` (extension) | champ `seasonalWeatherAsset` (slot Inspector « Seasonal Weather Asset ») | → asset `SeasonalWeather_Mortagne.asset` (dossier `Assets/_Project/Data/Weather/`). |

**Composant sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| `MonthSelectorBinding` (E2) | `Runner` → `_Bootstrap/SimulationRunner`. `DropdownField` UXML par défaut `starting-month-dropdown` (+ `Label` `starting-month-hint`) dans la section « Conditions initiales » du scenario panel. |

**Nouveau RC** :

| Asset | Producteur | Consommateurs |
|---|---|---|
| (aucun nouveau RC — la saisonnalité est consommée via `EcosystemModel.CurrentWeather` existant, étendu) | — | — |

### Chantier E3 — Carbone sol + EddyTower (livré 2026-05-29)

**Extension `_Bootstrap/SimulationRunner`** :

- Nouveau slot `Soil Carbon Container` → `RC_SoilCarbonStock.asset`
  (créer via `Create > Bocage > Data > RC_SoilCarbonStock`, ranger
  dans `Assets/_Project/Data/RuntimeContainers/`).
- Le `EddyTowerSensorReader` est instancié par le runner en `Awake`
  (et reconstruit en lockstep dans `Rebuild`) — pas de GameObject
  dédié à câbler.

**Sliders ajoutés au scenario panel** (pas de binding dédié) :

| Component | Champs sérialisés |
|---|---|
| `ScenarioControlsBinding` (élargi) | Les 2 sliders UXML `cover-crops-slider` (0-100 %) et `residue-restitution-slider` (0-100 %) ont été ajoutés à `Dashboard.uxml` dans la section « Décisions quotidiennes ». Les noms UXML par défaut sont câblés en SerializeField — aucune action drag-and-drop nécessaire. Le binding pousse via `ScenarioContext.CoverCropsCoveragePercent.SetTarget` / `ResidueRestitutionPercent.SetTarget` avec `transitionDurationDays` (cohérent avec les 5 sliders existants). |

### Chantier E4 — Faune visible 4 espèces

**Nouveau GameObject** :

| GameObject | Components | Références à brancher |
|---|---|---|
| `_Scene_Visual/Fauna` | `FaunaPool` (composant Couche 05) | `Placement` → asset `FaunaPlacement.asset`. `Spawn Root` → ce GameObject lui-même (ou enfant dédié `Fauna_Pool`). |
| Chaque pool member (pré-instancié au Awake par `FaunaPool`) | `SpriteRenderer`, `FaunaTraversalMotion` | Configuré automatiquement par `FaunaPool.BuildPool` à partir de `FaunaSpeciesDefinition` ; pas de réglage manuel. |

**Nouveau composant sur `_UI_Canvas`** :

| Component | Champs sérialisés à brancher |
|---|---|
| `FaunaPoolBinding` (E4) | `Pool` → `_Scene_Visual/Fauna` (GameObject portant `FaunaPool`). `Biodiv Composite` → `RC_BiodiversityComposite.asset`. `Master Seed` → même seed que `SimulationRunner` (pour cohérence cross-run). **Pas de Habitat/Water/Inputs en MVP** : observation `RC_FaunaFactor*` non activée tant qu'aucune sensibilité par-espèce n'est calibrée — extensible sans casser l'API. |

**Assets ScriptableObject** (dans `Assets/_Project/Data/Fauna/`) :

| Asset | Notes |
|---|---|
| `FaunaSpecies_Swallow.asset` | MotionMode **Traversal**. 3 sous-sprites du `swallow_sheet`, 8 fps wing flap, seuil 0.30, λ_max 0.108, **2 trajectoires** (haut + bas), max 2 hirondelles à l'écran. |
| `FaunaSpecies_Owl.asset` | MotionMode **Traversal**. 3 sous-sprites du `owl_sheet`, 6 fps wing flap, seuil 0.40, λ_max 0.042, **1 trajectoire** médium. `defaultFacesRight: false` (orienté gauche). |
| `FaunaSpecies_Buzzard.asset` | MotionMode **Traversal**. 3 sous-sprites du `buzzard_sheet`, 2 fps planar, seuil 0.50, λ_max 0.036, **1 trajectoire** haut+lent. `defaultFacesRight: false`. Remplace l'ancien `FaunaSpecies_Harrier` (correction mouette mal nommée, ADR #49). |
| `FaunaSpecies_Heron.asset` | MotionMode **StaticAppearance**. 1 sous-sprite du `heron.png` pré-existant, seuil 0.65, `staticPosition (2.5, -2.93)` au bord de la mare, `fadeDurationSec 1.5`. Pas de trajectoire, pas de wing flap. Le binding active/désactive l'alpha selon biodiv vs seuil ; le composant `FaunaStaticAppearance` gère le lerp. |
| `FaunaPlacement.asset` | SO racine listant les 4 espèces ci-dessus. |

### Chantier E5 — Capital + biodiv 3 facteurs (livré 2026-05-29)

**Nouveaux RC à créer en `Assets/_Project/Data/RuntimeContainers/`** :

| Asset | Producteur | Consommateurs |
|---|---|---|
| `RC_FaunaFactorHabitat.asset` | `SimulationRunner` | `OngletBiodivBinding` (E6), `FaunaPoolBinding` (E4). |
| `RC_FaunaFactorWater.asset` | `SimulationRunner` | idem. |
| `RC_FaunaFactorInputs.asset` | `SimulationRunner` | idem. |
| `RC_TotalInvestment.asset` | `SimulationRunner` | `OngletEconomieBinding` (E6). |
| `RC_InvestmentHorizon.asset` | `SimulationRunner` | idem. |

**Action utilisateur Unity (5 RC à créer + brancher)** :

1. Dans `Assets/_Project/Data/RuntimeContainers/`, créer 5 assets via
   `Create > Bocage > Data > RC_FaunaFactorHabitat` (idem Water,
   Inputs, TotalInvestment, InvestmentHorizon). Renommer chacun selon
   la colonne « Asset » ci-dessus.
2. Sélectionner `_Bootstrap/SimulationRunner` dans la hiérarchie.
   Sous la nouvelle rubrique Inspector **« Capital & horizon
   (chantier E5 / ADR #50) »**, glisser :
   - `RC_TotalInvestment.asset` → champ `Total Investment Container`.
   - `RC_InvestmentHorizon.asset` → champ `Investment Horizon Container`.
3. Sous la nouvelle rubrique **« Biodiv 3 facteurs (chantier E5 / ADR #51) »**,
   glisser :
   - `RC_FaunaFactorHabitat.asset` → `Fauna Factor Habitat Container`.
   - `RC_FaunaFactorWater.asset` → `Fauna Factor Water Container`.
   - `RC_FaunaFactorInputs.asset` → `Fauna Factor Inputs Container`.
4. Tous les 5 slots sont optional — si laissés null, la simulation
   tourne sans erreur (seul l'affichage onglets E6 et le binding popup
   manquent leurs sources). Conseil : brancher dès maintenant pour
   ne pas avoir à y revenir au moment de E6.

Pas de nouveau GameObject, pas de nouveau binding sur `_UI_Canvas`
(le popup `DecisionPopupBinding` existant a été étendu pour afficher
la ligne « Coût upfront estimé : X €/ha » sous le slider PlantHedges
— aucune référence Inspector supplémentaire requise, le label UXML
`decision-popup-investment` est trouvé via `Q<Label>`).

**Diff Dashboard.uxml** : ajout d'un `<ui:Label name="decision-popup-investment" class="decision-popup-investment hidden" />` entre le
slider magnitude et la rangée de boutons.

**Diff Dashboard.uss** : ajout de la règle `.decision-popup-investment`
(bordure haut + mono crème, cohérent avec
`.decision-popup-magnitude-value`).

### Chantier E6 — Panneau inspection capteurs + onglets Niveau B (livré 2026-06-02)

Câblage scène final tel que livré. Le détail des sous-étapes B.1→B.4
est dans `ROADMAP.md` §8 ; ici on liste uniquement les actions
manuelles Unity à reproduire sur une scène vierge.

**Configuration scène (Main.unity)** :

- **Aucun ajout manuel sur les sprites capteurs**.
  `SensorVisualPlacer.BuildFrom()` ajoute automatiquement aux 5
  sprites : `BoxCollider2D`, `SensorMetadataTag`, `SensorHoverEmitter`,
  `SensorHoverHighlight`, et désormais `SensorClickHandler`.
- **Pas de `Physics2DRaycaster` à ajouter sur la caméra** :
  `OnMouseDown` legacy fire directement sur le `Collider2D` (même
  path que le hover éprouvé depuis des mois).
- L'EventSystem Unity n'est pas requis pour ce chantier (le bus de
  clic est statique, pas EventSystem).

**Nouveaux composants sur `_UI_Canvas`** (déjà détaillés dans le
tableau §3 ci-dessus) :

| Component | Action utilisateur |
|---|---|
| `OngletBiodivBinding` | Add Component + brancher 4 RC + Fauna Pool. |
| `OngletClimatBinding` | Add Component + brancher Runner + `RC_SoilCarbonStock`. |
| `OngletEconomieBinding` | Add Component + brancher Runner + 2 RC investissement. |
| `NiveauBModalsBinding` | Add Component. Aucun champ à régler. |
| `SensorInspectorPanelBinding` | Add Component + brancher Runner. |

**UXML / USS** : `Dashboard.uxml` et `Dashboard.uss` enrichis en
place. Aucun fichier UXML/USS séparé créé — tout vit dans le
dashboard. Nouvelles classes USS principales : `.level-b-trigger`,
`.level-b-modal-overlay/.card/.header/.title/.close`,
`.sensor-inspector-overlay/.card/.header/.title/.close/.subtitle/
.chart-row/.axis-column/.axis-label/.chart-host/.footer-info`,
`.sensor-chart`.

**Décision force-online (ADR #57)** : la liste « Capteurs déployés »
affiche les 5 dots en vert (Online) indépendamment de la valeur
`OnlineStatus` du SO. La légende online/deferred a été retirée du
UXML. Le champ `OnlineStatus` est préservé dans
`SensorPlacementDefinition` et `SensorMetadataTag` pour réactivation
ultérieure (item backlog « capteur en panne »).

### Chantier E7 — Polish + publication

Pas de nouveau câblage scène. Configuration build only (Crunch
DXT5 conditionnel sur sprites lourds, cf `docs/ASSETS_LIST.md` §6
étape 7).

---

## Mémo synthèse — nouveaux composants sur `_UI_Canvas` post-recadrage

| Component | Chantier | ADR cadrant | Statut |
|---|---|---|---|
| `MonthSelectorBinding` | E2 | #52 | livré |
| `FaunaPoolBinding` | E4 | #49 | livré |
| `OngletBiodivBinding` | E6 | #54 | livré |
| `OngletClimatBinding` | E6 | #54 | livré |
| `OngletEconomieBinding` | E6 | #54 | livré |
| `NiveauBModalsBinding` | E6 | #54 (pivot UX) | livré |
| `SensorInspectorPanelBinding` | E6 | #53 | livré |

Au total : 7 nouveaux components sur `_UI_Canvas` post-recadrage,
tous livrés à l'issue de E1-E6. Le `WeatherNormalsPanelBinding`
initialement envisagé pour E6 a été absorbé dans
`SensorInspectorPanelBinding` (les normales mois courant/suivant
sont calculées dans le footer du layout WeatherStation, via le
helper statique pur `MonthlyExpectedPrecipitationMm`). Aucune
restructuration de la hiérarchie scène (les 7 racines préfixées `_`
restent inchangées, conformes `CLAUDE.md` §8).

E8/E9 n'ont ajouté **aucun nouveau composant `_UI_Canvas`** mais ont
modifié `TechDeltaLabelBinding`, `OngletEconomieBinding`,
`DecisionPanelBinding`, `DecisionPopupBinding` et `ManualActionsBinding`
(voir Journal des modifications, entrées E8/E9).
