# SIMULATION_OVERVIEW.md — Vulgarisation scientifique du modèle

Document de vulgarisation du modèle de simulation pour audience non
développeur (agroécologue, jury M1, recruteur curieux du fond
scientifique). Lecture cible : 15-20 minutes.

Créé le 2026-05-28 — version alignée sur le scope MVP verrouillé
(cf `CLAUDE.md` §17 et `ROADMAP.md` chantiers E1-E9).

Mis à jour 2026-06-04 : aligné sur E1-E9 (réconciliation E8 refonte
delta tech + E9 système de recommandation).

---

## 1. Objet de la simulation

Le projet `bocage-digital-twin` est un **digital twin d'un site
bocager fictif mais plausible du Perche normand**, instrumenté de
5 capteurs et muni d'un moteur de décision agroécologique.

Il sert un objectif portfolio : démontrer la capacité à construire
une simulation temps réel d'agroécosystème, avec une architecture
logicielle propre (5 couches strictement découplées), une calibration
sourcée publiquement (INRAE, Solagro, Météo-France, AFAC,
Légifrance), et une chaîne pédagogique complète capteur → événement
→ recommandation → arbitrage → effet économique-écologique. Depuis
E9, ce moteur va jusqu'aux **contre-recommandations économiques**
(anti-greenwashing) et distingue les recos « gagnant-gagnant »
(popup proactif) des arbitrages de compromis (liste passive).

**Ce qu'il n'est pas** : un modèle scientifique validé pour décision
opérationnelle. Les ordres de grandeur sont défendables, mais aucun
paramètre n'a été validé par un agronome ou hydrologue du Perche pour
ce site précis.

---

## 2. Thèse défendue

**Tester honnêtement la convergence éco/écolo** via instrumentation
et monétisation des services écosystémiques, **sans postuler le
résultat**.

L'utilisateur, mis dans la peau d'un agriculteur du Perche, peut :

- Régler le contexte exogène (anomalies climatiques RCP4.5,
  politiques publiques MAEC + PSE, intensité d'arrachage de haies,
  intensité d'intrants).
- Agir manuellement (planter des haies, irriguer ponctuellement).
  La baisse d'intensité d'intrants n'est plus une action ponctuelle :
  c'est désormais un changement de pratique soutenu via le slider
  d'intensité d'intrants (recommandation automatique `ReduceInputs`).
- Accepter ou refuser des recommandations algorithmiques déclenchées
  par les événements détectés par les capteurs.

À tout moment, le DT compare la trajectoire **réelle** (avec les
décisions de l'agriculteur appliquées) à la trajectoire **fantôme**
du « paysan passif » : mêmes seeds, mêmes conditions exogènes
(climat, MAEC, PSE), mais décisions de l'agriculteur figées à leur
valeur de lancement. L'écart mesure l'apport des décisions et de
l'instrumentation, jamais postulé : il peut être positif, neutre ou
négatif selon le scénario.

---

## 3. Architecture en 5 couches

Le modèle est structuré en **5 couches d'asmdef strictement
empilées** :

| Couche | Rôle | Dépendances |
|---|---|---|
| 01 — Simulation Core | Modèle biophysique pur C#, sans Unity | aucune |
| 02 — Sensors | Lecture bruitée du modèle + détection d'événements | 01 |
| 03 — Decision | Recommandations, projections d'issues, journal | 01, 02 |
| 04 — Indicators | Agrégation en KPIs, shadow run, reporter | 01, 02, 03 |
| 05 — Presentation | Unity (MonoBehaviour, shaders, UI Toolkit) | 01-04 |

Les couches inférieures **ne référencent jamais** les couches
supérieures. La Couche 01 est entièrement testable en EditMode sans
Unity. C'est ce qui permet d'avoir une suite de tests unitaires
déterministes sur la biophysique.

Détails dans `docs/ARCHITECTURE.md`.

---

## 4. Modèle biophysique (Couche 01)

### 4.1 Variables d'état principales

| Variable | Unité | Plage typique | Source de calibration |
|---|---|---|---|
| `HedgerowDensity` | m/ha | 60-110 | INRAE, PNR Perseigne |
| `WaterTableDepth` | m | 0.5-6 | ordres de grandeur bocage clay-bottomed |
| `CropYield` | t/ha | 3-8 | Agreste Eure-et-Loir 2015-2024 |
| `InputCost` | €/ha/an | 100-2000 | CIVAM, AFPF |
| `MaintenanceCost` | €/ha/an | 90 (1 €/m × 90 m/ha) | Réseau Haies 2024 |
| `FaunaPopulation` | indice [0, 1.5] | normalisé | INRAE Vigie-Nature |
| `CurrentWeather` | T° °C + précip mm | saisonnier (E2) | Météo-France Mortagne |
| `SoilCarbonStock` | tC/ha | 50 (default) → 80-120 (équilibre) | INRAE 4 pour 1000, Solagro |

Détails complets dans `docs/CALIBRATION.md`.

### 4.2 Règles de dynamique

Chaque variable est mise à jour à chaque tick (1 tick = 1 jour
simulé) via une règle dédiée :

- `HedgerowGrowthRule` : croissance + arrachage selon scenario.
- `WaterTableDynamicsRule` : infiltration (précip) − évaporation
  (T°), avec inertie.
- `WeatherUpdateRule` : T° et précip tirées sur la base des normales
  mensuelles Météo-France + anomalies scenario + bruit stochastique
  (cf §5 saisonnalité).
- `CropYieldDynamicsRule` : cible EMA (constante de temps 100 jours)
  combinant effet bell-curve densité haies, effet hydrique, effet
  climatique, et réponse **concave** à l'intensité d'intrants
  (quadratique-plateau / Mitscherlich : −2,8 % à I=0.8, −17,5 % à
  I=0.5, plafond +5 % à I=2.0).
- `InputCostDynamicsRule` : cible EMA (60 jours) combinant
  intensification (part variable 30 % / part fixe 70 % : seuls les
  intrants opérationnels suivent l'intensité), MAEC, climat.
- `FaunaDynamicsRule` : 3 facteurs explicites (habitat, eau, intrants)
  avec effets faibles canicule et carbone sol (cf §6 biodiv).
- `SoilCarbonDynamicsRule` : modèle 1-pool `dC/dt = inputs − k·C`,
  `k = 1/40 an⁻¹` (cf §7 carbone sol).

### 4.3 Optimum de profit émergent

La réponse **concave** du rendement à l'intensité d'intrants (§4.2)
combinée à la **séparation 30/70 du coût** (seuls 30 % du coût
suivent l'intensité) fait émerger un **maximum de profit intérieur** :
l'optimum se situe autour de I* ≈ 0,8, et non aux extrêmes. Au-delà,
le rendement plafonne pendant que le coût continue de grimper ; en
deçà, la perte de rendement n'est pas compensée par l'économie
d'intrants (dont 70 % sont fixes).

Conséquence pédagogique honnête : l'**extensification profonde**
(I très bas) est ~neutre, voire légèrement négative, sur le profit
brut de production. Sa valeur ne vient pas du rendement mais des
**services écosystémiques** (biodiversité accrue) et des **aides**
(MAEC, PSE, écorégime) — c'est précisément ce que le DT met en
lumière.

---

## 5. Saisonnalité (chantier E2 livré post-recadrage)

### 5.1 Données mensuelles Météo-France

Le modèle utilise les **normales climatologiques 1991-2020** de la
station Météo-France de **Mortagne-au-Perche (Orne, 61)** : 12 valeurs
moyennes de température + 12 valeurs cumul mensuel précipitations.

Ces valeurs sont encodées dans un ScriptableObject
`SeasonalWeatherDataAsset` (Couche 01).

### 5.2 Modèle stochastique Niveau 3

À chaque tick, la météo est tirée selon un modèle à 2 étages :

1. **Pluie / pas pluie** — chaîne de Markov ON/OFF mensuelle :
   tirage Bernoulli(`p_wet[mois]`), avec `p_wet` allant de ~0.20 en
   juillet à ~0.55 en novembre.
2. **Intensité de pluie** — si pluvieux, tirage log-normal
   (`mu[mois]`, `sigma[mois]`) calibré sur séries journalières.
3. **Température** — T_jour = T_mois + bruit gaussien (σ = 2 °C),
   avec anomalie scenario additive (par exemple +2 °C en RCP4.5).

Tous les tirages utilisent un `SeededRandom` avec sous-flux dédiés
(`"markov-rain"`, `"weather-noise"`) pour garantir la reproductibilité
et l'indépendance des sources d'aléa.

### 5.3 Cascade saisonnière

L'introduction d'un cycle saisonnier dans `WeatherUpdateRule`
propage automatiquement de la saisonnalité dans :

- `WaterTableDynamicsRule` (recharge hivernale, baisse estivale).
- `HedgerowGrowthRule` (croissance modulée par l'hydrologie).
- `FaunaDynamicsRule` (pénalité canicule au-delà d'un seuil T°).
- `CropYieldDynamicsRule` + `InputCostDynamicsRule` étendus à la
  météo journalière (canicule WeatherStation → effet économique
  direct).

---

## 6. Biodiversité (chantier E5 livré post-recadrage)

L'indice composite biodiversité est calculé en agrégeant 3 facteurs
exposés explicitement :

| Facteur | Signification | Source |
|---|---|---|
| Habitat | densité bocagère normalisée | Constant et al. 1976 |
| Eau | qualité ressource hydrique (profondeur nappe) — la mare partagera ce facteur quand #16 sera livré | Hallmann 2017 |
| Intrants | pression chimique inversée | MNHN 2024 |

Effets faibles additionnels :

- **Canicule** : pénalité au-delà de seuil T° journalier (Hallmann
  2017 — déclin insectes Krefeld).
- **Carbone sol** : bonus si stock C > seuil (sols vivants = plus
  de macrofaune).

Un 4ème facteur **Diversité paysage** (Shannon-like sur prairies
permanentes et diversité cultures) est documenté en backlog
(item #20) mais non livré dans le MVP.

La densité de faune visible (héron, chouette, busard, hirondelle)
est pilotée par l'indice composite + les facteurs individuels : les
espèces apparaissent et disparaissent à l'écran selon leur seuil
de tolérance.

---

## 7. Carbone sol (chantier E3 livré post-recadrage)

### 7.1 Modèle 1-pool

```
dC/dt = inputs − k·C
```

Avec :

- `C` = `SoilCarbonStock` (tC/ha), default 50.
- `k = 1/40 an⁻¹` (constante de minéralisation, demi-vie ~28 ans,
  INRAE 4 pour 1000).
- `inputs` = somme journalière de :
  - couverts d'interculture (paramètre scenario, 0-100 %),
  - restitution des résidus de récolte (paramètre scenario,
    0-100 %),
  - apport haies (proportionnel à `HedgerowDensity`).

À l'équilibre, `C_eq = inputs / k`, soit typiquement 80-120 tC/ha pour
des sols bien gérés (Solagro Afterres 2050).

### 7.2 Capteur EddyTower

La **tour de covariance** présente dans la scène mesure le **flux
net journalier CO2/CH4** avec bruit gaussien. Le panneau d'inspection
au clic affiche le flux journalier + le stock cumulé.

C'est la chaîne capteur → indicateur affiché, sans événement ni reco
— conforme au principe « capteur bout-en-bout » qui peut s'arrêter
à un indicateur affiché (cf `CLAUDE.md` §17 principe directeur).

---

## 8. Capteurs et incertitude (chantiers E2, E3, E6 livrés)

Le DT comporte **5 capteurs**, chacun bout-en-bout (mesure →
indicateur OU événement → recommandation) :

| Capteur | Mesure | Chaîne aval |
|---|---|---|
| Piezometer | `WaterTableDepth` + bruit | événement `DroughtProlonged` → reco `IrrigationAdvice` |
| AcousticSensor | abondance faune bruitée (σ ∝ 1/√fauna) | fusionné avec CameraTrap → événement `FaunaAcousticAnomaly` → reco `ReduceInputs` |
| CameraTrap | abondance faune bruitée | idem AcousticSensor (fusion `FaunaSensorReader`) |
| WeatherStation | T° + précip + bruit gaussien | lecture pure → indicateur affiché (T° glissante, précip glissantes) |
| EddyTower | flux CO2/CH4 + bruit | lecture pure → indicateur affiché (stock C cumulé) |

Chaque capteur est **cliquable** dans la scène. Un clic ouvre un
panneau d'inspection qui affiche les graphes des 365 derniers jours
de mesure vs références (normales mensuelles, seuils d'alerte,
vérité modèle pour visualiser le bruit).

C'est l'une des contributions pédagogiques clés du DT : rendre
visible l'**incertitude de mesure** propre à chaque technologie
de capteur, plutôt que de présenter des valeurs « propres »
trompeuses.

---

## 9. Moteur de décision (chantiers E1 → E9 cumulatifs)

### 9.1 Recommandations algorithmiques

Le `RecommendationEngine` (Couche 03) consomme les événements
détectés et produit des recommandations. Après E9, le système couvre
**8 recommandations sur 6 leviers**, avec un **dispatch sensible à
l'état** (le moteur choisit le levier qui dispose encore de marge de
manœuvre) :

| Événement déclencheur | Recommandation(s) | Levier |
|---|---|---|
| `DroughtProlonged` | Irrigation | irrigation ponctuelle |
| `FaunaAcousticAnomaly` | `ReduceInputs` **ou** `ReduceHedgeRemoval` **ou** `PlantHedges` | levier avec marge disponible |
| `SoilCarbonLowEvent` (nouveau, seuil 45 tC/ha) | `SowCoverCrops` **ou** `RestoreResidue` | couverts / résidus |
| `LowProfitabilityEvent` (nouveau, seuil 50 €/ha) | `RaiseInputs` **ou** `IncreaseHedgeRemoval` | intensification / arrachage |

E9 introduit **2 nouveaux événements** : `SoilCarbonLowEvent`
(stock carbone sol sous 45 tC/ha) et `LowProfitabilityEvent`
(rentabilité intégrée sous 50 €/ha, assorti d'un **garde-fou
biodiversité ≥ 0.30** : on ne recommande de ré-intensifier ou
d'arracher que si la biodiversité n'est pas déjà fragile).

Ces dernières recommandations sont des **contre-recommandations
économiques** assumées (anti-greenwashing) : le moteur ne pousse pas
seulement vers l'extensification ; quand la rentabilité décroche, il
propose honnêtement de ré-intensifier ou d'arracher, dans la limite
du garde-fou biodiversité. Le DT ne postule pas que « plus vert =
toujours mieux ».

Chaque recommandation porte :

- Un `OutcomeProjector` qui projette 2 horizons (30 j et 365 j) sous
  forme de 3 points (worst / expected / best).
- Un texte Rationale au pattern uniforme : phrase d'action concrète
  + ligne `Effet modélisé : ...` chiffrée sur les variables
  effectivement touchées + ligne `Déclenché par : ...` indiquant
  l'événement source.

Côté présentation, E9 distingue deux modes de remontée : les recos
**gagnant-gagnant** s'affichent en **popup proactif** (on attire
l'œil de l'utilisateur), tandis que les arbitrages de **compromis**
(notamment les contre-recommandations économiques) restent dans une
**liste passive** que l'utilisateur consulte s'il le souhaite.

### 9.2 Actions manuelles via journal

L'utilisateur peut aussi déclencher des actions manuelles via
2 boutons :

- Planter des linéaires de haies (avec slider densité).
- Irrigation ponctuelle (avec slider intensité).

La baisse d'intensité d'intrants n'est plus un bouton ponctuel : elle
est désormais portée par le **slider d'intensité d'intrants** (un
changement de pratique soutenu, plancher 0.5, transition lissée sur
10 jours), proposé via la recommandation automatique `ReduceInputs`.

Chaque action manuelle est **journalisée comme
`IRecommendation` auto-acceptée** (cf ADR #47). Elle traverse le même
`AutoActionPipeline` que les recos algo, garantissant traçabilité
totale.

### 9.3 Capital et horizon de rentabilité (chantier E5 livré)

Les actions « Planter des haies » portent un **coût upfront**
(densité × prix au m linéaire, 3-10 €/m, source Réseau Haies de
France).

Le `DecisionJournal` cumule l'investissement total. L'indicateur
`InvestmentHorizonIndicator` **latche le premier jour où la valeur
NETTE de la techno** (gain cumulé réel − fantôme, moins
l'investissement upfront) **franchit 0** — le jour d'amortissement.
Tant qu'aucun investissement n'existe, l'horizon reste « Sans objet ».

C'est l'argument décisif d'un agriculteur réel : pas « est-ce que
c'est bénéfique à long terme ? », mais « **en combien d'années est-ce
que j'amortis ?** » — standard du métier (Chambre d'agriculture,
référentiel MAEC).

---

## 10. Simulation fantôme (shadow run)

Une seconde trajectoire — le **fantôme** — tourne **en parallèle**
avec :

- Le **même seed maître** (mêmes tirages stochastiques).
- Un **modèle neuf + un scénario « baseline figée »**
  (`ScenarioContext.CreateFrozenShadowFrom`) : les conditions
  exogènes (climat, MAEC, PSE) restent partagées, mais les décisions
  de l'agriculteur sont **figées à leur valeur de lancement**.
- À chaque pas, le fantôme **rejoue les règles** contre l'état
  exogène partagé **sans ré-avancer le scénario**
  (`TickWithoutAdvancingScenario`).

Toute divergence d'état entre réel et fantôme provient donc
**uniquement** des décisions de l'utilisateur qui s'écartent de la
baseline figée.

Le KPI **« Apport de la techno »** = **valeur NETTE cumulée en
€/ha** : intégrale jour-après-jour de l'écart de rentabilité
(réel − fantôme) depuis le jour 0, **moins l'investissement upfront
des actions** (les coûts capteurs ne sont pas comptés). Positif si la
stratégie tech rapporte plus qu'elle ne coûte, négatif sinon. Le jour
où la valeur nette franchit 0 = l'**horizon de rentabilité**.

Cette simulation fantôme est la **garantie d'honnêteté** du DT : le
résultat n'est pas postulé, il émerge des choix de l'utilisateur
dans un scénario donné.

---

## 11. Aides PAC et PSE intégrées

L'indicateur `IntegratedProfitabilityIndicator` inclut explicitement
les aides publiques actuelles :

- **DPB Hexagone** : 127,67 €/ha (Légifrance).
- **Paiement redistributif** : 48 €/ha (sur les 52 premiers ha).
- **Écorégime base** : 45 €/ha (remplace le paiement vert PAC depuis
  2023, arrêté Légifrance du 25 novembre 2025).
- **Bonus haies PAC 2025** : 20 €/ha (Chambre Agriculture Pays de
  la Loire).
- **PSE** : 0 à 1 €/m de haie/an (paramètre scenario, calibré
  Villeneuve-en-Perseigne 92 €/tCO₂).
- **MAEC** : réduction d'intrants jusqu'à 30 % (paramètre scenario,
  CIVAM Haut-Bocage).

Cette intégration est l'**amortisseur principal du revenu agricole
français** (RICA Agreste 2024). Sans ces aides, la majorité des
fermes céréalières seraient déficitaires.

---

## 12. Limites honnêtes du modèle

Cette section liste les **limites assumées** du modèle, pour
éviter toute survalorisation.

### Limites résolues par le MVP post-recadrage

- ✅ ~~Pas de saisonnalité météo~~ — résolu par E2 (Markov + normales
  Mortagne-au-Perche).
- ✅ ~~EddyTower sans réalité dans le modèle~~ — résolu par E3
  (carbone sol 1-pool).
- ✅ ~~WeatherStation sans Reader formel~~ — résolu par E2 (lecture
  pure bout-en-bout).
- ✅ ~~Faune scalaire abstraite~~ — résolu par E4 (4 espèces visibles
  + 3 facteurs exposés en E5).
- ✅ ~~Actions tech sans coût upfront~~ — résolu par E5 (capital +
  horizon rentabilité).
- ✅ ~~Onglets Niveau B vides~~ — résolu par E6.

### Limites assumées (post-MVP, cf `docs/BACKLOG.md`)

- **Biodiversité = 3 facteurs**. Le 4ème facteur Diversité paysage
  (Shannon-like prairies + cultures) est documenté en backlog
  (#20).
- **Phénologie cultures simplifiée**. Le rendement évolue via EMA
  sans semis ni récolte explicites. Phénologie complète (GDD,
  fenêtres semis/récolte, stress reproductif) en backlog (#18).
- **Santé végétale absente**. Aucun pathogène ni ravageur modélisé
  (chalara purgé pour cohérence — soit on remet tout l'écosystème
  santé végétale d'un coup, soit rien, cf #17).
- **Hydrologie schématique**. Modèle d'infiltration/évaporation
  empirique non validé hydrologiquement.
- **Pas de gestion mare différenciée**. Le sprite mare est piloté
  par la nappe, mais une dynamique propre + événements
  d'assèchement amphibiens est en backlog (#16).
- **Pas d'aléa de mortalité plantation**. 30-50 % des plants meurent
  en réalité dans les 3 premières années. En backlog (#15).
- **Pas de recommandations préventives**. Les recos sont toutes
  réactives (seuil franchi → alerte). Recos anticipatives en backlog
  (#13).
- **Mix cultures figé**. 70 % blé / 30 % colza dans
  `CropYieldDynamicsRule`. Levier diversification en backlog (#14).

### Limites de scope assumées (jamais comblées dans ce projet)

- **Site fictif mais plausible** : le bocage modélisé est inventé. Il
  s'inspire du Perche normand sans correspondre à une parcelle
  réelle.
- **Calibration de niveau moyen** : ordres de grandeur défendables
  via sources publiques (INRAE, Solagro, Agreste, Météo-France,
  AFAC, Légifrance), mais aucun audit par un agronome ou hydrologue
  du Perche.
- **Pas une publication scientifique** : démonstrateur portfolio,
  pas un modèle prédictif utilisable opérationnellement.

---

## 13. Sources et calibration

Toutes les constantes du modèle sont sourcées publiquement et
documentées dans `docs/CALIBRATION.md`. Les sources principales :

- **INRAE** — Données bocage, calibration biodiv (Vigie-Nature),
  4 pour 1000 carbone sol.
- **Solagro** — Afterres 2050 (couverts, rotations diversifiées,
  carbone sol).
- **Météo-France** — Normales climatologiques 1991-2020 station
  Mortagne-au-Perche (61).
- **Agreste** — Rendements Eure-et-Loir 2015-2024 (blé, colza).
- **CIVAM** — Économies intrants en système herbager extensif.
- **AFPF** — Coûts intrants grandes cultures.
- **Réseau Haies / AFAC-Agroforesteries** — Référentiel coûts
  gestion haies 2024, prix plantation 3-10 €/m.
- **Chambre Agriculture Pays de la Loire** — Bonus haie PAC 2025.
- **Légifrance + Leandri Conseils 2025** — Décomposition CAP basic
  payment 2023-2027.
- **Hallmann et al. 2017** (Krefeld) — Déclin insectes.
- **Constant et al. 1976** — Réseau bocager passereaux.
- **MNHN 2024** — État de la biodiversité française.
- **OFB / RMT Zones humides** — Limnologie petites mares.
- **PNR du Perche** — Protocoles de suivi et calibrations locales.

---

## 14. Pour aller plus loin

- Architecture détaillée : `docs/ARCHITECTURE.md`.
- Décisions de design : `docs/DECISIONS.md`.
- Calibration paramètre par paramètre : `docs/CALIBRATION.md`.
- Roadmap MVP : `docs/ROADMAP.md`.
- Backlog post-MVP : `docs/BACKLOG.md`.
- Câblage Unity scène : `docs/SCENE_WIRING.md`.
- Pièges WebGL anticipés : `docs/WEBGL_GOTCHAS.md`.
- Spec opérationnelle : `CLAUDE.md` (racine, niveau projet).
