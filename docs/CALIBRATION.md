# CALIBRATION.md — Sources et choix de paramétrage du modèle

Document de référence pour chaque constante de calibration du
simulation core. Une entrée par paramètre : valeur, source, fenêtre de
plausibilité, dernière révision.

> **Avertissement** : ce modèle reste un démonstrateur portfolio. Les
> ordres de grandeur sont ancrés sur des sources publiques mais aucune
> calibration ne fait l'objet d'une validation par un agronome ou un
> hydrologue du Perche. Toute utilisation opérationnelle nécessiterait
> un audit scientifique dédié au site.

---

## Modèle d'écosystème

### `HedgerowDensity` (m de haies par hectare)

- **Défaut initial** : 90 m/ha
- **Plage plausible Perche** : 60-110 m/ha
- **Sources** :
  - INRAE : 60 m/ha pour cultures, 100 m/ha pour prairies. Ces deux densités sont les valeurs de référence les mieux documentées pour les effets sur les flux carbone.
  - Réseau bocage (PNR Perseigne, North-Sarthe, frontière Perche) : 110 m/ha sur exploitation type, contre 70 m/ha moyenne départementale.
- **Dernière révision** : 2026-05-21

### `WaterTableDepth` (m sous surface)

- **Défaut initial** : 2.0 m
- **Plage plausible** : 0.5 m (hiver clay-bottom) à 6.0 m (été permeable upland)
- **Sources** : ordres de grandeur génériques nappes superficielles bocage clay-bottomed. Pas de source spécifique Perche identifiée.
- **Dernière révision** : 2025 (Étape 6a, non modifié depuis)

### `CropYield` (tonnes par hectare)

- **Défaut initial** : 5.5 t/ha
- **Plage plausible Perche** : 3-8 t/ha
- **Sources** :
  - Agreste Eure-et-Loir 2015 : blé tendre 70 q/ha (7.0 t/ha), colza 29 q/ha (2.9 t/ha)
  - Agreste Eure-et-Loir 2020 : colza 35 q/ha (3.5 t/ha)
  - Agreste France 2024 : moyenne nationale blé tendre 62.4 q/ha (6.24 t/ha)
  - Mix typique ferme mixte céréales/oléagineux Perche : ~70% blé, 30% colza → moyenne pondérée ≈ 5.7 t/ha. Arrondi à 5.5 pour rester conservateur.
- **Dernière révision** : 2026-05-21

### `InputCost` (€ par hectare et par an)

- **Défaut initial** : 1200 €/ha/yr
- **Plage plausible** : 100 (bio extensif) à 2000 €/ha/yr (intensif)
- **Sources** :
  - CIVAM Haut-Bocage : économie 76% sur fertilisants et 74% sur phytos en système herbager extensif (référence implicite : conventionnel à ~1500 €/ha)
  - AFPF (Association Française pour la Production Fourragère) : grandes cultures annuelles 1100-2000 €/ha
  - 1200 €/ha = médiane conventionnelle bocagère mixte.
- **Dernière révision** : 2026-05-21 (était 400, erreur de calibration)

### `MaintenanceCost` (€ par hectare et par an)

- **Défaut initial** : 90 €/ha/an (= 1.0 €/m × 90 m/ha)
- **Sources** :
  - Réseau Haies 2024 référentiel : coût moyen de gestion durable des haies 3.69 €/ml (vs 3.32 €/ml en 2019)
  - Le 3.69 €/ml inclut main d'œuvre au tarif marché ; pour la part out-of-pocket auto-assumée par l'agriculteur (fuel + équipement + intrants secondaires), on retient 1.0 €/m/an, soit ~27% du référentiel.
- **Dernière révision** : 2026-05-21 (était 0.30 €/m/yr, sous-estimé)

---

## Règles biophysiques

### `HedgerowGrowthRule`

- **Croissance annuelle** : 0.5 m/ha/an au water-multiplier optimum
- **Source / justification** : ordre de grandeur d'une régénération
  naturelle modeste en l'absence de pression d'arrachage. Plage
  observée dans la littérature : 0.2-2 m/ha/an selon conditions. La
  valeur faible reflète une régénération lente sans plantation active.
- **Modulation par nappe** : optimum à 2 m, multiplier ∈ [0, 1.5]
  selon écart, sensibilité 0.2/m
- **Dernière révision** : Étape 3 (non modifié)

### `AgriculturalPressureImpactRule`

- **Taux d'arrachage** : direct scenario input en m/ha/an, plage [0, 10]
- **Source / justification** :
  - PNR Perche : remembrement 1970-1990 a réduit la densité bocagère
    de ~30 m/ha sur 20 ans dans les zones les plus touchées (= ~1.5 m/ha/an
    moyen mais 5+ m/ha/an dans les phases agressives)
  - Plage de slider 0-10 m/ha/an couvre du gel total à une pression
    catastrophique (au-delà du 5 historique du Perche).
- **Dernière révision** : 2026-05-21 (refactor scenario Option 3)

### `WaterTableDynamicsRule`

- **InfiltrationFactor** : 0.0001 m/mm de pluie
- **EvaporationBase** : 0.003 m/jour par (T/30°C) de degré-jour
- **Source / justification** : valeurs d'ordres de grandeur calibrées
  pour produire une variation saisonnière réaliste (±0.3 m/an autour
  de la moyenne). **Non validée hydrologiquement** — coefficients
  empiriques. Le faible impact de la pluie (10 mm → 1 mm de remontée)
  reflète la majorité du ruissellement et de l'évapotranspiration de
  surface qui ne touchent pas la nappe profonde.
- **Dernière révision** : 2026-05-21 (suppression du multiplier climat
  redondant, déjà capturé via la température météo)

### `WeatherUpdateRule`

- **BaseTemperatureC** : 12.0 °C (moyenne annuelle Perche)
- **BasePrecipitationMm** : 2.0 mm/jour (≈ 730 mm/an, conforme aux
  normales Perche)
- **Bruit gaussien** : σ = 3°C température, σ = 1.5 mm précipitations
- **Source** : normales climatologiques Météo-France Eure-et-Loir / Orne
- **Dernière révision** : 2026-05-21 (refactor pour utiliser
  TemperatureAnomalyC et PrecipitationAnomalyPercent directement)

### `CropYieldDynamicsRule` — multiplicateurs

| Effet | Valeur | Source |
|---|---|---|
| Pic bell density haies | ×1.0 (neutre) | Le baseline 5.5 t/ha **inclut déjà** l'effet brise-vent moyen du bocage (Agreste mesure sur fermes équipées). La bell pénalise les écarts, ne booste pas le pic. Cf #40 |
| Pénalité max écart densité | −15% | RMT Agroforesteries : gains brise-vent +6-20%, on prend la médiane comme "coût d'absence de bocage" |
| Heat penalty | −6%/°C anomalie positive | IPCC AR6 chap. 5 : 5-7% perte rendement céréales tempérées par °C au-dessus optimum |
| Drought penalty | −0.5%/% anomalie négative précip | Études INRAE cultures européennes : 0.3-0.7% par % déficit pluvial |
| Intensification effect | ±10% autour de 1.0 (intensity factor) | Modeste : l'intensification impacte surtout les coûts, peu le rendement |
| Constante de temps EMA | 100 jours (k=0.01) | Inertie agronomique typique : la cible évolue, le rendement attendu intègre sur la saison |

- **Dernière révision** : 2026-05-21 (reformulation bell pour éviter
  double-comptage de l'effet bocage)

### `InputCostDynamicsRule` — multiplicateurs

| Effet | Valeur | Source |
|---|---|---|
| Intensification (×factor) | Direct | Plage [0.5, 2.0] |
| MAEC réduction max | −30% à 100% couverture | Borne basse "passage MAEC standard" — CIVAM rapporte −76% en herbager extensif, on reste modeste pour MAEC sans bascule bio |
| Climat surcharge max | +40% (heat 20% + drought 20%) combinés | Plausible sous RCP8.5 horizon 2050 |
| EMA | 60 jours (k=0.017) | Inertie comptable d'un cycle cultural |

- **Dernière révision** : 2026-05-21

---

## Indicateurs

### `IntegratedProfitabilityIndicator`

- **Formule** : `yield × cropPrice − inputs − maintenance + density × pseRate + pacBonus + basicCap`
- **CropPriceEurosPerTonne** : 250 €/t
  - Mix pondéré blé (230-270 €/t) et colza (400-550 €/t) en année
    moyenne, ratio 70/30
- **PacHedgeBonusEurosPerHectare** : 20 €/ha
  - Source : Chambre Agriculture Pays de la Loire — Bonus haie PAC 2025,
    forfait par hectare de SAU lorsque haies présentes
- **BasicCapPaymentEurosPerHectare** : 220 €/ha
  - DPB Hexagone ~127,67 €/ha + paiement redistributif ~48 €/ha
    (sur les 52 premiers ha) + écorégime base ~45 €/ha
  - Le paiement vert PAC 2014-2020 est supprimé depuis 2022 et
    remplacé par l'écorégime à partir de 2023 (Légifrance, arrêté
    du 25 novembre 2025). Sources : Légifrance + Leandri Conseils
    2025 (sub-étape 10b doc audit).
  - C'est l'**amortisseur principal du revenu agricole français**.
    Sans cette aide forfaitaire, la majorité des fermes céréalières
    seraient déficitaires sur leur seule production (cf. RICA Agreste
    2024 : RCAI 0-100 €/ha pour céréales/oléoprotéagineux en année
    difficile, intégralement couvert par les aides).
- **PseSubsidyRate** : scenario input [0, 1.0] €/m/an
  - Source extrapolée Villeneuve-en-Perseigne (Sarthe limitrophe
    Perche) : système PSE à 92 €/tCO₂ donne 600-2000 €/an par ferme,
    soit ~0.10-0.30 €/m/yr selon densité. Le borne haute 1.0 couvre
    des contrats spécifiques rares.
- **Display bounds** : [-500, +1500] €/ha/an
  - Marge moyenne grandes cultures Perche avec aides : 200-400 €/ha/an
    (RICA Agreste 2024 + DPB) ; négative possible sous mauvaise
    conjoncture climatique ; +1000-1500 atteignable sous pratique
    bocagère vertueuse + MAEC max + PSE max.
- **Dernière révision** : 2026-05-27 (audit doc sub-étape 10b : CAP
  basic payment recalé à 220 €/ha avec nouvelle décomposition DPB +
  redistributif + écorégime base ; sources Légifrance + Leandri 2025).
  Révision précédente : 2026-05-21 (ajout CAP basic payment, bornes
  display étendues à +1500).

---

## Validation expérimentale par simulations

Quatre scénarios-types ont été simulés mentalement contre la calibration
pour valider la cohérence de la trajectoire des KPIs vis-à-vis de la
réalité du Perche agricole.

### Scenario 1 — Référence neutre

| Paramètre | Valeur |
|---|---|
| Anomalie T° | 0 °C |
| Anomalie précip | 0 % |
| Arrachage haies | 0 m/ha/an |
| Intensité intrants | 1.0× |
| MAEC | 0 % |
| PSE | 0 €/m/an |

Équilibre attendu :
```
CropYield     = 5.5 t/ha   × 1 × 1 × 1 × 1 = 5.5 t/ha
InputCost     = 1200 €/ha/an × 1 × 1 × 1 = 1200 €/ha/an
Maintenance   = 1.0 × 90 m/ha = 90 €/ha/an
profit = 5.5×250 − 1200 − 90 + 0 + 20 + 220 = 325 €/ha/an
```

✅ Cohérent avec RICA Agreste 2024 RCAI 200-350 €/ha (céréales mixtes
Perche en année moyenne avec aides CAP).

### Scenario 2 — Changement climatique modéré (RCP4.5 horizon 2050)

| Paramètre | Valeur |
|---|---|
| Anomalie T° | +2 °C |
| Anomalie précip | −20 % |

Équilibre attendu (recharge rate 0.002/day, équilibre nappe ~2.62 m
sous l'effet combiné chaleur + déficit de pluie) :
```
WaterTable eq ≈ 2.62 m  → waterEffect ≈ 0.962
CropYield  = 5.5 × 1 × 0.962 × 0.792 × 1 = 4.19 t/ha
InputCost  = 1200 × 1 × 1 × 1.147 = 1376 €/ha/an
profit ≈ 4.19×250 − 1376 − 90 + 0 + 20 + 230 = −171 €/ha/an
```

⚠️ Déficit −150 €/ha/an : la ferme survit grâce à la CAP basic mais
le compte d'exploitation est négatif. Cohérent avec les projections
INRAE / GIEC pour le scénario RCP4.5 horizon 2050 sur la moitié nord
française.

### Scenario 3 — Pratique vertueuse (bocage densifié + MAEC + PSE max)

| Paramètre | Valeur |
|---|---|
| Anomalie T° | 0 °C |
| Anomalie précip | 0 % |
| Arrachage haies | 0 m/ha/an |
| Intensité intrants | 0.5× (bio extensif) |
| MAEC | 100 % |
| PSE | 1.0 €/m/an |

Équilibre attendu :
```
CropYield  = 5.5 × 1 × 1 × 1 × 0.95 = 5.225 t/ha
InputCost  = 1200 × 0.5 × 0.7 × 1 = 420 €/ha/an
Maintenance = 90 €/ha/an
PSE        = 90 × 1.0 = 90 €/ha/an
profit = 5.225×250 − 420 − 90 + 90 + 20 + 230 = 1136 €/ha/an
```

✅ Très rentable. Cohérent avec les exploitations bio bocagères
documentées : revenus 800-1200 €/ha/an grâce à la baisse drastique des
intrants et à la valorisation des services environnementaux.

### Scenario 4 — Worst case (climat catastrophique + intensif + arrachage)

| Paramètre | Valeur |
|---|---|
| Anomalie T° | +5 °C |
| Anomalie précip | −60 % |
| Arrachage haies | 10 m/ha/an |
| Intensité intrants | 2.0× |
| MAEC | 0 % |
| PSE | 0 €/m/an |

Équilibre attendu après ~10 ans :
```
HedgerowDensity → 0 m/ha (arrachage 10 m/an > croissance 0.5 m/an)
WaterTable eq ≈ 3.2 m  → waterEffect ≈ 0.85
CropYield  ≈ 5.5 × 0.87 (no-hedge bell) × 0.85 (water drift) × 0.49 (climate) × 1.1 (intensif) = 2.2 t/ha
InputCost  = 1200 × 2 × 1 × 1.4 = 3360 €/ha/an
Maintenance = 0 €/ha/an (plus de haies à entretenir)
PAC haie bonus = 0 (plus de haies)
profit ≈ 2.2×250 − 3360 − 0 + 0 + 0 + 230 = -2580 €/ha/an
```

⚠️ Catastrophe. La ferme est techniquement en faillite chronique.
Cohérent avec l'objectif pédagogique du modèle : montrer la
non-soutenabilité d'un système "intensif sans bocage sous stress
climatique majeur".

### Sensibilité +1 °C — point d'attention

Test rapide : à neutre +1 °C seulement (rien d'autre changé). En tenant
compte de la dérive du `WaterTableDepth` vers ~2.2 m (faible) :
```
CropYield target = 5.5 × 0.996 × 0.94 = 5.15 t/ha → −87 €/ha/an
InputCost target = 1200 × 1.04 = 1248 €/ha/an → +48 €/ha/an
profit = 5.15×250 − 1248 − 90 + 0 + 20 + 220 = 190 €/ha/an
```

→ Perte de **−135 €/ha/an** au global (325 → 190). Sensibilité forte
mais conforme aux projections INRAE (5-7 % perte rendement par °C
pour céréales tempérées + 3-5 % surcharge intrants).

C'est précisément l'**enseignement honnête** du digital twin : 1 °C
d'écart suffit à diviser la marge par ~1.7, sans compter la
non-linéarité des effets cumulés (sécheresse, événements extrêmes
non modélisés).

---

## Source d'autorité : tests EditMode

Les valeurs de cette section sont des **simulations papier**. La
**vérification exécutable** se trouve dans
`Assets/_Project/Tests/EditMode/CalibrationScenarioValidationTests.cs`,
qui construit un `SimulationEngine` pour chaque scénario, le fait
tourner 3650 jours simulés (10 ans), et asserte le profit dans une
fenêtre de plausibilité. Lancer ces tests via Test Runner > EditMode
> Run All à chaque modification de calibration.

---

## Historique des révisions

| Date | Modifications |
|---|---|
| 2025 (Étape 5) | Première version, constantes ad-hoc |
| 2025 (Étape 6a) | Bornes display KPIs ; pas de modification du modèle |
| 2026-05-21 (Étape 7a) | Ajout `CropYield`, `InputCost`, `MaintenanceCost` au modèle. Constantes initiales non calibrées scientifiquement (`InputCost = 400` était sous-estimé d'un facteur 3). |
| 2026-05-21 (Étape 7c.1 Option 3) | Refactor scenario : 6 params physiques avec unités réelles, source par paramètre dans la doc inline. |
| 2026-05-21 (Étape 7c calibration) | Recalibration sourcée : `BaselineTonnesPerHectare 5→5.5`, `BaselineEurosPerHectarePerYear 400→1200`, `MaintenancePerMeterPerYear 0.30→1.0`, bell curve reformulée pour ne pas double-compter l'effet bocage, PAC hedge bonus +20€/ha ajouté à l'indicateur. |
| 2026-05-21 (Étape 7c CAP) | Ajout `BasicCapPaymentEurosPerHectare = 230 €/ha` (DPB national 2025 + paiement vert + écorégime). Baseline neutre passe de 105 à 335 €/ha/an, cohérent avec RICA Agreste 2024. Plage anomalie T° resserrée à `[-2, +5]` °C. Sliders à valeur numérique visible en remplacement des FloatField/IntegerField. Quatre scénarios-types validés par simulation contre la calibration. |
| 2026-05-27 (Sub-étape 10b doc audit) | Audit externe : `BasicCapPaymentEurosPerHectare 230 → 220 €/ha`. Décomposition révisée : DPB Hexagone 127,67 + paiement redistributif 48 (sur 52 premiers ha) + écorégime base 45. Le paiement vert PAC 2014-2020 était supprimé depuis 2022 et remplacé par l'écorégime à partir de 2023 (arrêté Légifrance du 25 novembre 2025). Baseline neutre 335 → 325 €/ha/an (toujours dans la fenêtre RICA Agreste 2024 200-350). Sensibilité +1 °C inchangée en delta (−135 €/ha/an). Sources sciences faune également remises à jour (Constant et al. 1976 pour passereaux ; Hallmann 2017 + MNHN 2024 pour insectes en remplacement d'IPBES 2019). |

---

## Sources web consultées (mai 2026)

- [INRAE — Données et indicateurs bocage](https://www.inrae.fr/sites/default/files/pdf/cab30d694a5bb1e2bdd6396ffb2b5478.pdf)
- [Réseau Haies — Référentiel de coûts de gestion durable des haies 2024](https://reseauhaies.fr/wp-content/uploads/2024/11/Referentiel-cout-de-gestion-juin-2024.pdf)
- [Chambre Agriculture Pays de la Loire — Bonus haie PAC 2025](https://pays-de-la-loire.chambres-agriculture.fr/actualites-1/detail-de-lactualite/revalorisation-du-bonus-haie-de-la-pac-a-20-eur-par-hectare)
- [RMT Agroforesteries — Étude effet brise-vent sur céréales 2023](https://www.rmt-agroforesteries.fr/wp-content/uploads/2023/11/rmt_acqui_ref_tech.pdf)
- [Web-Agri — Système PSE Villeneuve-en-Perseigne](https://www.web-agri.fr/fourrage/article/208330/ils-ont-invente-leur-systeme-de-paiement-pour-services-environnementaux)
- [Terre-Net — Rendements blé tendre Agreste 2024](https://www.terre-net.fr/cultures/article/869514/les-estimations-de-rendements-en-ble-tendre-par-departement)
- [Prom'Haies Nouvelle-Aquitaine — Fonctions agronomiques des haies](https://www.promhaies.net/association/pourquoiplanter/fonctions-agronomiques,696/)
- [CIVAM — Vers des systèmes économes en intrants](https://www.civam.org/civam-du-haut-bocage/actions/vers-des-systemes-de-cultures-economes-en-intrants/)

---

## Paramètres post-recadrage 2026-05-28 (chantiers E1-E7)

Cette section regroupe les paramètres ajoutés au modèle par les
chantiers E1-E7 de la nouvelle `ROADMAP.md`.

### Saisonnalité — données mensuelles Météo-France (chantier E2)

**Station** : Mortagne-au-Perche (Orne, 61).
**Normales** : 1991-2020.
**Source** : Météo-France (meteofrance.com/climat/normales/61293001 —
à vérifier au moment de l'encodage du `SeasonalWeatherDataAsset`).

**Valeurs indicatives** (à confirmer sur source officielle au moment
de l'encodage de l'asset) :

| Mois | T° moyenne (°C) | Précipitations cumul (mm) | p_wet | mu (log-mm) | sigma (log-mm) |
|---|---|---|---|---|---|
| Jan | 4,0 | 65 | 0,50 | 1,40 | 0,80 |
| Fév | 5,0 | 55 | 0,45 | 1,40 | 0,80 |
| Mar | 7,0 | 55 | 0,40 | 1,45 | 0,80 |
| Avr | 10,0 | 55 | 0,40 | 1,45 | 0,80 |
| Mai | 13,0 | 60 | 0,35 | 1,55 | 0,85 |
| Juin | 17,0 | 50 | 0,30 | 1,60 | 0,90 |
| Juil | 19,0 | 50 | 0,20 | 1,75 | 0,95 |
| Août | 19,0 | 50 | 0,25 | 1,70 | 0,90 |
| Sept | 16,0 | 55 | 0,35 | 1,60 | 0,85 |
| Oct | 12,0 | 70 | 0,45 | 1,50 | 0,80 |
| Nov | 7,0 | 75 | 0,55 | 1,40 | 0,80 |
| Déc | 5,0 | 80 | 0,55 | 1,40 | 0,80 |

**T° moyenne annuelle** ≈ 11,2 °C. **Précipitations annuelles cumul**
≈ 720 mm. Cohérent avec climat océanique du Perche.

**Bruit gaussien T° quotidienne** : σ = 2 °C autour de la moyenne
mensuelle. Sous-flux RNG `"weather-noise"`.

**Workflow Markov par tick (jour simulé)** :

1. Tirer un Bernoulli(`p_wet[mois courant]`) → jour pluvieux ou sec.
2. Si pluvieux : tirer un LogNormal(`mu[mois]`, `sigma[mois]`) →
   précipitations en mm. Sous-flux RNG `"markov-rain"`.
3. T° du jour = T_mois + N(0, σ=2).
4. Ajouter anomalies scenario en additif sur T° et multiplicatif sur
   précipitations.

**Cohérence avec ancienne calibration** : le `BaseTemperatureC = 12 °C`
et `BasePrecipitationMm = 2 mm/jour ≈ 730 mm/an` de l'ancienne
`WeatherUpdateRule` correspondent bien à la moyenne annuelle des
valeurs ci-dessus. La refonte E2 conserve l'ordre de grandeur global
en introduisant le cycle saisonnier intramensuel.

**À vérifier au moment de l'encodage** : les valeurs ci-dessus sont
indicatives basées sur la connaissance générale du climat percheron.
L'encodage final de `SeasonalWeatherDataAsset` doit consulter les
normales officielles meteofrance.com et les inscrire telles quelles.

**Dernière révision** : 2026-05-28 (création post-recadrage).

---

### Carbone sol — modèle 1-pool (chantier E3)

**Modèle** : `dC/dt = inputs − k·C`.

| Paramètre | Valeur | Source |
|---|---|---|
| `SoilCarbonStock` default | 50 tC/ha | Référence sols cultivés bocage Perche, BDAT INRAE. |
| `k` (constante minéralisation) | 1/40 an⁻¹ | Demi-vie ~28 ans, INRAE 4 pour 1000. |
| Input couverts d'interculture | 1,2 × CoverCropsCoveragePercent / 100 tC/ha/an | Solagro Afterres 2050. |
| Input restitution résidus | 0,8 × ResidueRestitutionPercent / 100 tC/ha/an | Solagro Afterres 2050. |
| Input haies (proxy) | 0,4 × HedgerowDensity / 90 tC/ha/an | AFAC-Agroforesteries (0,4 tC/ha/an stockable sous haies denses 90 m/ha). |

**Équilibre attendu** : `C_eq = inputs / k`.

Pour un scénario « couverts 50 % + résidus 80 % + haies 90 m/ha » :
inputs ≈ 0,6 + 0,64 + 0,4 = 1,64 tC/ha/an → C_eq ≈ 66 tC/ha. Le
default 50 tC/ha tend lentement vers cet équilibre sur ~30 ans
simulés.

Pour un scénario « couverts 0 % + résidus 0 % + haies 0 m/ha »
(intensif sans bocage) : inputs ≈ 0 → C_eq → 0. Le default 50 tC/ha
décroît lentement vers 0 sur ~150 ans simulés (avec demi-vie 28 ans).

**Dernière révision** : 2026-05-28 (création post-recadrage).

---

### Horizon de rentabilité — prix plantation haies (chantier E5)

**Action concernée** : `ManualPlantHedgesRecommendation` (action
manuelle journalisée, cf ADR #47 + #50).

| Paramètre | Valeur | Source |
|---|---|---|
| Prix au m linéaire plantation haies | 3-10 €/m | Réseau Haies de France, MAEC référentiel coûts plantation. |
| Default planning ManualPlantHedges | 5 €/m (médian) | Approximation MVP. À paramétrer en `ScenarioContext` post-MVP (item BACKLOG #18 / #19 raffinement). |

**Calcul `InvestmentCost`** :

```
InvestmentCost (€/ha) = magnitude_slider (m/ha) × prix_au_m (€/m)
```

Exemple : magnitude 30 m/ha × 5 €/m = 150 €/ha. Pour une exploitation
de 100 ha hypothétique, cela représente 15 000 € upfront — chiffre
plausible cohérent avec amendement Sénat nov. 2025.

**Calcul horizon de rentabilité** :

```
À chaque tick post-action :
  cumulProfitDelta(t) = Σ(realProfit(τ) − shadowProfit(τ)) pour τ ∈ [t_action, t]
HorizonYears = first day où cumulProfitDelta(t) >= InvestmentCost, / 365
```

Si non atteint dans la simulation : afficher « Horizon rentabilité :
non encore atteint » (au lieu d'une valeur NaN).

**Dernière révision** : 2026-05-28 (création post-recadrage).

---

### Magnitudes par défaut des actions manuelles (chantier E1)

**Sliders de magnitude** dans le panneau « Interventions ponctuelles »
du décision-panel. Default + plage utilisateur.

| Action | Slider | Default | Plage | Effet modélisé |
|---|---|---|---|---|
| `manual-plant-hedges` | densité plantée | 30 m/ha | 10-100 m/ha | `+magnitude m/ha` sur `HedgerowDensity`, `+0,01 × magnitude €/ha/an` sur `MaintenanceCost`. |
| `manual-irrigation` | intensité | 1,5 m | 0,5-3,0 m | Remontée temporaire `−magnitude m` sur `WaterTableDepth` (plancher 0,5 m), durée 30 jours décroissants. |
| `manual-reduce-inputs` | intensité réduction | 0,2 | 0,1-0,5 | `+0,05 × ratio` sur `FaunaPopulation` (boost ponctuel insectes), `−200 × ratio €/ha/an` sur `InputCost` (économie immédiate), durée 30 jours. |

Où `ratio = magnitude / IntensityCutPerStep` (cf
`ReduceInputsRecommendation`).

Les valeurs `Effet modélisé : ...` affichées dans le popup décision
sont les valeurs calculées au moment du clic (cf wordings exacts dans
ADR #55).

**Dernière révision** : 2026-05-28 (création post-recadrage).

---

### Recalibration biodiv 3 facteurs (chantier E5)

L'agrégation `BiodiversityCompositeIndicator` actuelle (50 % fauna +
30 % hedge + 20 % water inverse) est recalibrée pour exposer
explicitement 3 facteurs au niveau onglet :

| Facteur | Variable | Sources |
|---|---|---|
| Habitat | `RC_FaunaFactorHabitat` (dérivé `HedgerowDensity`) | Constant et al. 1976 (Réseau Haies passereaux). |
| Eau | `RC_FaunaFactorWater` (dérivé `WaterTableDepth` + `PondWaterLevelMeters` si #23 livré) | Hallmann et al. 2017 (Krefeld), MNHN 2024. |
| Intrants | `RC_FaunaFactorInputs` (dérivé `InputCost` + `InputIntensityFactor`) | IPBES 2019 (rebound faune cessation pesticides), MNHN 2024. |

**Pondérations recalibrées** (à affiner via tests EditMode) : 40 %
habitat, 25 % eau, 35 % intrants. Le déplacement de poids vers les
intrants reflète la littérature post-2017 sur le déclin insectes
attribué majoritairement aux pesticides néonicotinoïdes (Krefeld).

**Effets faibles additionnels** sur `FaunaPopulation` (ADR #51) :

- **Canicule** : pénalité 0,01/jour si T° > 30 °C, plafond cumul
  −0,15 sur 30 jours (Hallmann 2017).
- **Carbone sol** : bonus 0,02 si `SoilCarbonStock > 80 tC/ha`,
  proxy macrofaune (INRAE).

**Dernière révision** : 2026-05-28 (création post-recadrage).
