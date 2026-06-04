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

- **Modèle (depuis 2026-05-29, chantier E2 / ADR #52)** : chaîne de
  Markov ON/OFF par jour, intensité log-normale en cas de jour pluvieux,
  paramètres mensuels Mortagne-au-Perche encodés dans
  `SeasonalWeatherDataDefaults`. Détails complets et calibration
  source : §Saisonnalité ci-dessous.
- **Bruit gaussien T° quotidienne** : σ = 2 °C autour de la moyenne
  mensuelle. Sous-flux RNG `"weather-noise"`.
- **Bruit pluie** : porté par le tirage log-normale (pas de bruit
  gaussien additif). Sous-flux RNG `"markov-rain"`.
- **Source** : modélée 30 km (NEMS reanalysis) via
  planificateur.a-contresens.net, valeurs annuelles 10,77 °C / 720 mm
  cohérentes avec normales Météo-France couramment citées pour la zone.
- **Dernière révision** : 2026-05-29 (refonte saisonnière, chantier E2).
- **Historique** : avant E2, modèle à constantes annuelles
  (`BaseTemperatureC = 12 °C`, `BasePrecipitationMm = 2 mm/jour`),
  bruit gaussien σ = 3 °C / 1.5 mm. Refactor 2026-05-21 avait retiré
  ces bruits gaussiens (sans cycle saisonnier, du bruit pur sans
  structure aplatissait les seuils d'événements).

### `CropYieldDynamicsRule` — multiplicateurs

| Effet | Valeur | Source |
|---|---|---|
| Pic bell density haies | ×1.0 (neutre) | Le baseline 5.5 t/ha **inclut déjà** l'effet brise-vent moyen du bocage (Agreste mesure sur fermes équipées). La bell pénalise les écarts, ne booste pas le pic. Cf #40 |
| Pénalité max écart densité | −15% | RMT Agroforesteries : gains brise-vent +6-20%, on prend la médiane comme "coût d'absence de bocage" |
| Heat penalty | −6%/°C anomalie positive | IPCC AR6 chap. 5 : 5-7% perte rendement céréales tempérées par °C au-dessus optimum |
| Drought penalty | −0.5%/% anomalie négative précip | Études INRAE cultures européennes : 0.3-0.7% par % déficit pluvial |
| Intensité → rendement | **Concave** (quadratique-plateau / Mitscherlich) : −2,8 % à I=0.8, −17,5 % à I=0.5, plateau au-dessus (+5 % à I=2.0). `effet = 1 − 0.70·(1−I)²` sous 1.0 | Lechenet 2017 (Nature Plants 3:17008) ; méta-analyses bio Ponisio 2015 / de Ponti 2012 / Seufert 2012. Recalibré E9, cf §E8-E9 |
| Constante de temps EMA | 100 jours (k=0.01) | Inertie agronomique typique : la cible évolue, le rendement attendu intègre sur la saison |

- **Dernière révision** : 2026-05-21 (reformulation bell pour éviter
  double-comptage de l'effet bocage)

### `InputCostDynamicsRule` — multiplicateurs

| Effet | Valeur | Source |
|---|---|---|
| Intensité → coût | **30 % variable / 70 % fixe** : `coût = 1200 × (0.30·I + 0.70)`. Seule la part opérationnelle (engrais+phytos+semences) suit l'intensité | Observatoire Arvalis-Unigrains/CerFrance via FranceAgriMer (440 €/ha opérationnel ≈ 30 % des charges, 2020) + RICA-SSP. Recalibré E9 |
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
CropYield  = 5.5 × 0.825 (intensité 0.5, réponse concave E9) = 4.54 t/ha
InputCost  = 1200 × (0.30×0.5 + 0.70) × 0.7 (MAEC) = 1200 × 0.85 × 0.7 = 714 €/ha/an
Maintenance = 90 €/ha/an
PSE        = 90 × 1.0 = 90 €/ha/an
profit = 4.54×250 − 714 − 90 + 90 + 20 + 220 = 660 €/ha/an
```

✅ Solidement rentable — mais **plus le +1136 gonflé d'avant E9**. Le profit
vient désormais des **subventions** (MAEC + PSE max + CAP), pas d'intrants
« gratuits » : l'extensification coûte un vrai rendement (−17,5 %) et la part
fixe des coûts ne recule pas. C'est exactement la thèse — la valeur de la
pratique vertueuse passe par la biodiversité mesurée et les paiements pour
services environnementaux. Test EditMode : fenêtre [450, 900] €/ha/an.

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
CropYield  ≈ 5.5 × 0.87 (no-hedge bell) × 0.85 (water) × 0.49 (climate) × 1.05 (intensité 2.0, plateau E9) = 2.1 t/ha
InputCost  = 1200 × (0.30×2 + 0.70) × 1.4 (surcharge climat) = 1200 × 1.30 × 1.4 ≈ 2184 €/ha/an
Maintenance = 0 €/ha/an (plus de haies)
PAC haie bonus = 0 (plus de haies)
profit ≈ 2.1×250 − 2184 − 0 + 0 + 0 + 220 = -1440 €/ha/an
```

⚠️ Catastrophe — la ferme est en faillite chronique. Après E9 la
sur-intensification est **moins ruineuse côté coûts** (part variable 30 %
seulement : ~2184 €/ha au lieu de ~3360) ; la catastrophe vient désormais
surtout de l'effondrement du rendement (climat + perte du bocage). Test
EditMode : fenêtre < −800 €/ha/an. Objectif pédagogique tenu : montrer la
non-soutenabilité d'un système « intensif sans bocage sous stress
climatique majeur ».

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
| 2026-06-04 (E8-E9) | **Recalibration intrants** : rendement concave (était ±10 % linéaire ; Lechenet 2017 + méta-analyses bio) + coûts 30 % variable / 70 % fixe (était 100 % ; Arvalis/FranceAgriMer). Tue le « +980 » du KPI « apport de la techno », fait émerger un optimum de profit ≈ 0.8. Scénarios 3 (1136 → 660) et 4 (−2580 → ~−1440) recalés. **Système de recos équilibré** : 5 nouveaux leviers, dispatch state-aware, recos économiques anti-greenwashing (profit < 50 €/ha), surfaçage popup win/win vs liste « compromis ». Cf §E8-E9. |

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
- Lechenet et al. 2017, *Nature Plants* 3:17008, « Reducing pesticide use while preserving crop productivity and profitability on arable farms » (DOI 10.1038/nplants.2017.8)
- Seufert, Ramankutty & Foley 2012, *Nature* 485:229-232 ; de Ponti, Rijk & van Ittersum 2012, *Agric. Systems* 108:1-9 ; Ponisio et al. 2015, *Proc. R. Soc. B* 282:20141396 (écart de rendement bio↔conventionnel)
- Observatoire Arvalis-Unigrains / CerFrance via FranceAgriMer — structure de coûts de production grandes cultures (charges opérationnelles ~30 % vs charges de structure)
- INRAE — *Stocker du carbone dans les sols français* (étude « 4 pour 1000 », 2019) ; Solagro — *Afterres2050* ; Terres Inovia / Arvalis — services des couverts d'interculture

---

## Paramètres post-recadrage 2026-05-28 (chantiers E1-E7)

Cette section regroupe les paramètres ajoutés au modèle par les
chantiers E1-E7 de la nouvelle `ROADMAP.md`.

### Saisonnalité — données mensuelles modèlées Mortagne-au-Perche (chantier E2)

**Station** : Mortagne-au-Perche (Orne, 61).

**Source effectivement utilisée à l'encodage (2026-05-29)** :
`planificateur.a-contresens.net/europe/france/normandie/mortagne_au_perche/2991704.html` —
moyennes mensuelles dérivées du modèle global NEMS (résolution
30 km, reanalysis). Annual T° = 10,77 °C, annual cumul précip = 720,4 mm.
Cohérent avec les valeurs annuelles couramment citées par Météo-France
pour la zone (10,8 °C / 720 mm).

**Source officielle visée mais inaccessible** : le portail Météo-France
(meteofrance.com/climat/normales/61293001) renvoie un HTTP 404 au
2026-05-29. **TODO** post-MVP : récupérer les normales officielles
1991-2020 via le portail data.gouv.fr ou un export Météo-France
quand l'accès est restauré, comparer aux valeurs encodées et ajuster
si écart significatif (> 10 % sur précip mensuelles, > 0,5 °C sur
T° mensuelles).

**Valeurs encodées dans `SeasonalWeatherDataDefaults.MortagneAuPerche()`** :

| Mois | T° moyenne (°C) | Précipitations cumul (mm) | Jours pluie | p_wet | mu | sigma |
|---|---|---|---|---|---|---|
| Jan | 4,1 | 72,0 | 15 | 0,484 | 1,25 | 0,80 |
| Fév | 4,5 | 53,9 | 12 | 0,429 | 1,18 | 0,80 |
| Mar | 7,1 | 58,6 | 14 | 0,452 | 1,11 | 0,80 |
| Avr | 9,4 | 46,7 | 12 | 0,400 | 1,04 | 0,80 |
| Mai | 13,0 | 65,6 | 13 | 0,419 | 1,30 | 0,80 |
| Juin | 16,2 | 49,7 | 11 | 0,367 | 1,19 | 0,80 |
| Juil | 18,3 | 50,7 | 11 | 0,355 | 1,21 | 0,80 |
| Août | 18,2 | 42,7 | 10 | 0,323 | 1,13 | 0,80 |
| Sept | 15,1 | 53,3 | 11 | 0,367 | 1,26 | 0,80 |
| Oct | 11,4 | 75,1 | 14 | 0,452 | 1,36 | 0,80 |
| Nov | 7,1 | 66,8 | 14 | 0,467 | 1,24 | 0,80 |
| Déc | 4,8 | 85,3 | 15 | 0,484 | 1,42 | 0,80 |

**Méthode de dérivation des paramètres Markov** : pour chaque mois,
`p_wet = jours_pluie / jours_dans_le_mois` (Bernoulli direct) ;
`mu = ln(précip_mensuel / jours_pluie) − σ²/2` (la moyenne attendue de
la LogNormal `exp(mu + σ²/2)` retrouve par construction l'intensité
moyenne par jour pluvieux du mois) ; `σ = 0,80` est fixé constant à
travers les 12 mois (valeur typique des modèles d'intensité de pluie
journalière log-normale, plage 0,6-1,0 selon la littérature). Avec
ces paramètres, le cumul mensuel attendu
`jours × p_wet × exp(mu + σ²/2)` redonne par construction le cumul
observé.

**Bruit gaussien T° quotidienne** : σ = 2 °C autour de la moyenne
mensuelle. Sous-flux RNG `"weather-noise"`.

**Workflow Markov par tick (jour simulé)** :

1. Tirer un Bernoulli(`p_wet[mois courant]`) → jour pluvieux ou sec.
2. Si pluvieux : tirer un LogNormal(`mu[mois]`, `sigma[mois]`) →
   précipitations en mm. Sous-flux RNG `"markov-rain"`.
3. T° du jour = T_mois + N(0, σ=2).
4. Ajouter anomalies scenario : additif sur T°, multiplicatif sur
   précipitations.

**Cohérence avec ancienne calibration** : l'ancienne
`WeatherUpdateRule` (constantes 12 °C / 2 mm/jour ≈ 730 mm/an)
reproduit en première approximation les moyennes annuelles du nouvel
encodage (10,77 °C / 720 mm) — 1,2 °C plus chaude, 10 mm/an plus
humide, l'effet sur les fenêtres de tolérance des
`CalibrationScenarioValidationTests` (±60 €/ha sur le profit) est
contenu.

**Extension CropYield / InputCost à la météo journalière (ADR #52
option a)** :

| Effet | Paramètre | Valeur | Cap |
|---|---|---|---|
| Pénalité rendement par jour > 25 °C (fenêtre 30 j) | `HeatStressPenaltyPerDay` | 0,3 % / jour | 9 % |
| Surcharge intrants par jour > 25 °C (fenêtre 30 j) | `HeatStressSurchargePerDay` | 0,5 % / jour | 15 % |

Justification : la valeur seuil 25 °C est bien au-dessus de la
moyenne d'été locale (~18 °C) sans atteindre le seuil canicule
légal (30 °C, jamais observable sous +5 °C max d'anomalie réaliste à
Mortagne). À +5 °C d'anomalie + worst case (Scénario 4), juillet
moyen passe à 23,3 °C, les pics journaliers atteignent 26-29 °C,
le compteur atteint 5-15 j/mois et la pénalité reste modeste
(1,5-4,5 % sur rendement, 2,5-7,5 % sur intrants) — ce qui ne
casse pas la fenêtre de plausibilité du test (profit < -1500 €/ha).
Les termes sont additifs sur les pénalités d'anomalie scenario
préexistantes (`HeatPenaltyPerDegree`, `HeatSurchargePerDegree`),
représentant l'effet acute (canicule épisodique) à côté de l'effet
structurel (décalage moyen annuel).

**Dernière révision** : 2026-05-29 (livraison chantier E2 — encodage
réel + extension CropYield/InputCost + clarification source).

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

**Dernière révision** : 2026-05-29 (livraison chantier E3 —
implémentation `SoilCarbonDynamicsRule` Couche 01 + `EddyTowerSensorReader`
Couche 02 + `SoilCarbonIndicator` Couche 04 + RC observable + 2 sliders
Couche 05 + 8 tests EditMode).

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

> **Maj E8/E9** : le bouton manuel `manual-reduce-inputs` (pulse ponctuel
> +0,05 faune / −200 €/ha sur 30 j) a été **retiré**. Baisser les intrants
> est une pratique soutenue, pas un coup ponctuel : c'est désormais une
> **recommandation auto** qui abaisse le slider `InputIntensityFactor`
> (`ReduceInputsRecommendation`), plancher 0.5, transition 10 j. Seuls
> « planter des haies » et « irriguer » restent des boutons manuels.

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

**Pondérations recalibrées** (validées en chantier E5 le 2026-05-29
via tests EditMode `BiodiversityCompositeIndicatorTests`) : 40 %
habitat, 25 % eau, 35 % intrants. Le déplacement de poids vers les
intrants reflète la littérature post-2017 sur le déclin insectes
attribué majoritairement aux pesticides néonicotinoïdes (Krefeld).

Constantes correspondantes dans
`BiodiversityCompositeIndicator` : `HabitatWeight = 0.40`,
`WaterWeight = 0.25`, `InputsWeight = 0.35`. Bornes de
normalisation des 3 facteurs : habitat `[0.5, 1.4]`,
eau `[0.5, 1.0]`, intrants `[0.4, 1.1]` — alignées sur les sorties
des helpers `FaunaDynamicsRule.Compute{Habitat,Water,Inputs}Factor`.
Au baseline Perche (densité 90 m/ha, nappe 2 m, intensité 1.0) le
composite vaut ≈ 0.77 ; en hyper-bocage + bio extensif (intensité
0.5) il sature à 1.0 ; en intensification totale (intensité ≥ 2.4)
+ collapse habitat/eau il chute sous 0.05.

**Effets faibles additionnels** sur la cible fauna (ADR #51, livré
chantier E5) :

- **Canicule** : pénalité 0,01/jour si T° > 30 °C, plafond cumul
  −0,15 sur 30 jours (Hallmann 2017). Implémenté via le compteur
  `EcosystemModel.RecentCanicularDayCount` (miroir 30 j de
  `RecentHeatDayCount` au seuil 25 °C). Constantes
  `FaunaDynamicsRule.CanicularPenaltyPerDay = 0.01` et
  `CanicularPenaltyCap = 0.15`.
- **Carbone sol** : bonus +0,02 si `SoilCarbonStock > 80 tC/ha`,
  proxy macrofaune (INRAE BDAT). Step function — pas de
  lissage MVP. Constantes
  `FaunaDynamicsRule.SoilCarbonLivingThresholdTonnesPerHectare = 80.0`
  et `SoilCarbonBonus = 0.02`.

Ces modulateurs entrent dans la cible EMA de
`FaunaDynamicsRule.Apply` (additifs sur le produit
`baseline × habitat × eau × intrants`) — ils ne touchent PAS le
composite biodiv directement, qui reste fonction pure des 3 facteurs
normalisés. Ils affectent donc le visible faune E4 (via
`FaunaPopulation`) sans déformer l'indicateur Hero.

**Dernière révision** : 2026-05-29 (validé chantier E5).

---

## E8-E9 — Recalibration intrants + système de recommandations équilibré

Chantier livré 2026-06-04. Deux volets : (1) recalibrer la réponse économique
aux intrants pour qu'elle soit honnête et non « argent gratuit » ; (2) un
système de recommandations équilibré qui pousse vers un optimum — écolo OU
éco selon l'état — sans dogme ni greenwashing.

### Volet 1 — Recalibration de la réponse aux intrants

**Problème corrigé** : avant E9, le modèle rendait la baisse d'intrants quasi
gratuite (rendement −10 % linéaire seulement + coût 100 % variable), soit
+531 €/ha/an de profit pour une extensification totale — irréaliste, et en
contradiction directe avec les projections honnêtes de l'`OutcomeProjector`.
Le KPI « apport de la techno » grimpait jusqu'à +980 €/ha.

**1a. Rendement ↔ intensité : réponse CONCAVE (quadratique-plateau / Mitscherlich)**

`CropYieldDynamicsRule.ComputeIntensityEffect` :
```
si I ≤ 1.0 :  effet = 1 − 0.70·(1−I)²     (pénalité accélérée)
si I > 1.0 :  effet = 1 + 0.05·(I−1)       (plateau, rendements décroissants)
```
- I=0.8 (−20 % d'intrants) → −2,8 % de rendement
- I=0.5 (−50 %, plancher bio extensif) → −17,5 %
- I=2.0 (sur-fertilisation) → +5 % seulement

Sources :
- **Lechenet et al. 2017**, *Nature Plants* 3:17008 — sur 946 fermes arables
  françaises, −42 % de pesticides possibles sans perte de rendement ni de
  marge sur 59 % d'entre elles ; « low pesticide use rarely decreases
  productivity » (DOI 10.1038/nplants.2017.8).
- Écart bio↔conventionnel (borne haute de l'extensification) : **Ponisio
  2015** (*Proc. R. Soc. B*) −19,2 % ; **de Ponti 2012** (*Agric. Systems*)
  −20 %, **blé −27 %** ; **Seufert 2012** (*Nature*) −25 %.
- Forme concave (rendements décroissants de l'azote) : courbes Mitscherlich /
  quadratique-plateau, consensus agronomique.

**1b. Coût ↔ intensité : 30 % variable / 70 % fixe**

`InputCostDynamicsRule` : `coût = 1200 × (0.30·I + 0.70) × (1 − MAEC) × (1 + surcharge_climat)`.
Seule la part opérationnelle (~30 % : engrais + phytos + semences) suit
l'intensité ; la part de structure (~70 % : mécanisation, foncier, main
d'œuvre) ne recule pas quand on extensifie.

Source : **Observatoire Arvalis-Unigrains/CerFrance via FranceAgriMer**
(d'après RICA-SSP, ~4 000 exploitations) — « 440 €/ha de charges
opérationnelles ≈ 30 % des charges totales » (blé tendre, 2020).

**1c. Optimum de profit émergent ≈ 0.8**

La combinaison rendement-concave + coût-fixe/variable crée un **maximum
intérieur** du profit en fonction de l'intensité :
```
d(profit)/dI = 5.5·250·2·0.70·(1−I) − 1200·0.30 = 1925·(1−I) − 360 = 0
            →  I* ≈ 0.81
```
- **Au-dessus de ~0.8** (ex. conventionnel 1.0) : baisser les intrants
  augmente le profit ET la faune → **gagnant-gagnant** (reproduit Lechenet
  2017 : les fermes sur-appliquent).
- **En-dessous de ~0.8** (ex. bio extensif 0.5) : remonter les intrants
  augmente le profit au prix d'un peu de faune → **trade-off**.

L'extensification profonde est donc ~neutre à légèrement négative en profit
brut ; sa valeur passe par la biodiversité mesurée, les subventions (MAEC/PSE)
et le sol — ce que la techno révèle.

### Volet 2 — Système de recommandations équilibré

8 recos sur 6 leviers, chacune déclenchée par une **mesure** (§9), chacune
avec un **garde-fou de cohérence** (§17). Direction « écolo » (↑ biodiv) ou
« éco » (↑ profit) selon le levier et l'état.

| Reco | Levier | Déclencheur (mesure) | Dir. |
|---|---|---|---|
| Irriguer | `WaterTableDepth` | piézomètre → sécheresse prolongée | — |
| Baisser intrants | `InputIntensityFactor ↓` | capteur faune → anomalie acoustique | écolo |
| Planter des haies | `HedgerowDensity ↑` | faune + intrants au plancher | écolo |
| Réduire l'arrachage | `HedgeRemovalRate ↓` | faune + arrachage actif | écolo |
| Semer des couverts | `CoverCropsCoveragePercent ↑` | **tour Eddy → carbone sol bas** | écolo |
| Restituer les résidus | `ResidueRestitutionPercent ↑` | tour Eddy → carbone sol bas | écolo |
| **Remonter intrants** | `InputIntensityFactor ↑` | **profit < 50 €/ha** + sous l'optimum + faune OK | **éco** |
| **Éclaircir les haies** | `HedgeRemovalRate ↑` | profit < 50 + densité ≫ 90 + PSE faible | **éco** |

**Seuils** (constantes — à valider/affiner, calibration §2) :

| Seuil | Valeur | Justification |
|---|---|---|
| Plancher intensité | 0.5 (× réf.) | « bio extensif » −50 %. En deçà : hors plage calibrée. `ReduceInputsRecommendation.MinInputIntensityFactor` (partagé slider/clamp/garde-fou). |
| Optimum de profit | 0.8 (× réf.) | I* ≈ 0.81 dérivé (cf 1c). `RaiseInputsRecommendation.ProfitOptimalIntensityFactor`. |
| Carbone sol bas | 45 tC/ha | Sous le default 50 (dérive sous intrants organiques faibles). INRAE 4p1000 / BDAT. `EventDetector.SoilCarbonLowThresholdTonnesPerHectare`. |
| Rentabilité anormalement basse | 50 €/ha/an | Marges réelles Perche 100-400 ; neutre simulé ~325. Sous 50 = près de l'équilibre, ferme en réelle tension. `EventDetector.ProfitLowThresholdEurosPerHectare`. |
| Biodiversité critique | 0.30 (composite) | Sous le seuil d'apparition du 1er oiseau (E4). En deçà, le système refuse de troquer l'écologie pour du profit. `RecommendationEngine.BiodiversityCriticalThreshold`. |
| Haies surdenses | 120 m/ha | Bien au-dessus de l'optimum agronomique 90 (pic cloche rendement). `HedgeOverdenseThresholdMeters`. |
| PSE faible | 1.0 €/m/an | En deçà, une haie surdense peut coûter (entretien + rendement) plus qu'elle ne rapporte. `PseLowThresholdEurosPerMeter`. |
| Saturation habitat haies | 180 m/ha | Plafond du facteur habitat faune ; au-delà, planter plus n'aide plus. |

**Dispatch state-aware** (le « score ») : pour un signal, on choisit le levier
pertinent **avec de la marge**. Anomalie faune → baisser intrants (si marge,
levier le plus rapide — Hallmann 2017) → sinon réduire l'arrachage (si actif)
→ sinon planter → sinon silence (§17). Carbone bas → couverts → résidus.
Profit bas → remonter intrants (si sous l'optimum + faune OK) → sinon éclaircir
haies (si surdenses + PSE faible) → sinon silence.

**Surfaçage** (`RecommendationSurfacing`, classé par le signe des outcomes
projetés à 365 j) :
- **win/win** (profit ≥ 0 ET biodiv ≥ 0) → **popup** (interruption).
- **compromis** (une dimension se dégrade) → **liste passive** + marqueur
  « compromis ». Les recos éco (profit↑ biodiv↓) y atterrissent.
- **escalade** : un compromis *écologique* (biodiv↑ profit↓) remonte en popup
  si biodiv < 0.30. *Dormant* tant que l'`OutcomeProjector` est à coefficients
  figés (aucune reco écolo n'y est aujourd'hui classée « compromis »).

**Projections `OutcomeProjector`** (profit / biodiv attendus à 365 j) :

| Reco | Profit | Biodiv | Source |
|---|---|---|---|
| Couverts | +25 €/ha | +0.03 | INRAE 4p1000 (~0,31 tC/ha/an), Arvalis (30-100 kg N/ha) |
| Résidus | +18 | +0.02 | Solagro Afterres2050, INRAE |
| Réduire arrachage | +10 | +0.06 | INRAE/OFB (haie = corridor + PSE/PAC) |
| Remonter intrants | +60 | −0.06 | Lechenet 2017 + réponse concave |
| Éclaircir haies | +50 | −0.06 | cloche rendement + structure de coûts |

> **Limite documentée (incohérence Priority-1 de l'audit interne)** : ces
> projections restent des **coefficients figés**, pas des dérivations du modèle
> dans l'état courant. Bon ordre de grandeur et bon signe, mais elles peuvent
> diverger de l'effet réel (la baisse d'intrants promet +0,10 de biodiv long
> terme là où le modèle en donne ~+0,014 à un pas de −20 %). Les rendre
> *state-aware* (corriger l'incohérence + activer l'escalade) est en backlog.

**Vérification** : tests EditMode (`EconomicRulesTests`,
`CalibrationScenarioValidationTests`, `RecommendationEngineTests`,
`BalancedRecommendationsTests`, `RecommendationSurfacingTests`,
`EventDetectorTests`) — tous verts au 2026-06-04.

**Dernière révision** : 2026-06-04 (livraison E8-E9).
