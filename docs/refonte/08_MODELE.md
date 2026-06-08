# Refonte backend — 08 · Modèle biophysique (spec cible)

> **Statut** : SPEC AUTORITAIRE de la refonte — le **modèle cible à
> implémenter**. Distinct des docs décrivant le code *actuel*
> (`../ARCHITECTURE.md`, `../CALIBRATION.md`, `../SIMULATION_OVERVIEW.md`),
> qui restent valides jusqu'à l'implémentation. Trio de specs :
> **08 (modèle)** · 10 (moteur/KPI/décision) · 11 (vérification maths).
> **Base** : stocks → flux → couplages → leviers → KPI → objectif, après
> audit factuel de l'existant. Décisions verrouillées en §13.

---

## 0. Comment lire ce document

Chaque brique est étiquetée :

- **[GARDÉ]** — repris de l'existant tel quel (déjà correct).
- **[CHANGÉ]** — repris mais recâblé / recalibré.
- **[NOUVEAU]** — n'existe pas aujourd'hui.
- **[DÉCISION]** — fork ouvert : j'attends ton avis (récapitulés en §13).
- **[MODULE]** — bloc optionnel, avec note réalisme/coût pour doser le scope.

Les sources sont citées inline `(Auteur année)` et reprises en §10.

---

## 1. Corrections intégrées & invariants

Trois corrections de ta part, intégrées :

1. **Règle reco ⊆ leviers (corrigée).** Tout ce qui est actionnable dans
   une popup de recommandation l'est **aussi directement** (slider /
   contrôle), sans popup. Les recommandations sont un **sous-ensemble
   contextuel** des leviers — jamais une action exclusive. Un levier peut
   exister sans reco ; une reco ne peut pas exister sans levier direct.
2. **La haie n'est PAS un moteur.** [CHANGÉ] La haie devient un **proxy
   visuel de la santé de la flore**, pas une cause de rentabilité ni de
   quoi que ce soit. On supprime tous les couplages haie→rendement
   (effet brise-vent), haie→profit, haie→coût d'entretien. La haie *rend
   visible* un état (`F`, santé de la flore), elle ne le *produit* pas.
   [DÉCISION #1] : garder « densité de haie » comme KPI chiffré, ou
   afficher directement « santé de la flore / habitat » ?
3. **Trois entrées de simulation, et seulement trois** (§2).

**Invariants conservés du projet** : primauté du capteur (tout visuel
dérive d'une mesure/variable d'état) ; déterminisme par seed maître +
sous-flux ; run fantôme à baseline gelée (mêmes entrées → divergence
due aux seules décisions) ; couches 01-04 sans `UnityEngine`.

---

## 2. Architecture des entrées (les 3 seules entrées)

La simulation ne consomme **que** ces trois sources :

| Entrée | Rôle | Porté par |
|---|---|---|
| **A. Paramètres de lancement** | (a) fixent les **stocks initiaux** ; (b) **paramètrent les flux** (texture de sol, profondeur racinaire, scénario climat…) | écran de lancement → `SimulationConfig` [NOUVEAU] |
| **B. Décisions de l'agriculteur** | modulent les flux pendant le run (leviers, §7) | `ScenarioContext` [GARDÉ, étendu] |
| **C. Climatologie extraite du CSV** | calibre un générateur météo (stats mensuelles + persistance) | `WeatherGenerator` [CHANGÉ] |

**Conséquence majeure [CHANGÉ]** : la météo n'est **ni rejouée, ni lue
telle quelle**. On **génère** une météo synthétique réaliste avec un
**générateur stochastique seedé, calibré sur les statistiques du CSV**
(approche Richardson/WGEN — *Richardson 1981 ; Richardson & Wright 1984*).
Le CSV sert à *calibrer* (offline, via le script Python), jamais à être lu
au runtime.

Pourquoi générer plutôt que rejouer une année :
- **Pas de boucle** : chaque run produit une trajectoire météo neuve,
  statistiquement cohérente avec le climat réel du Perche.
- **Vraie persistance** : occurrence de pluie par **chaîne de Markov 2
  états** `{P(pluie|pluie), P(pluie|sec)}` → les épisodes secs/humides
  *collent* (corrige le défaut « Bernoulli sans mémoire » de l'existant) ;
  température en **moyenne saisonnière + anomalie AR(1)** → les vagues de
  chaleur émergent, au lieu d'être tirées indépendamment chaque jour.
- **Déterministe** : entièrement seedé. Même seed → même série. Run réel et
  run fantôme partagent le sous-flux météo → divergence 100 % due aux
  décisions.
- **Scénario climat** : perturbe les *paramètres* du générateur (§2.2).

### 2.1 Le générateur météo (spec)

Calibré par mois, à partir du CSV (offline) :
- **Occurrence pluie** : chaîne de Markov 1er ordre `{P01, P11}` par mois
  *(Stern & Coe 1984)* → persistance des épisodes.
- **Intensité des jours pluvieux** : loi log-normale `(μ, σ)` (déjà extraite
  par le script) ou gamma, par mois.
- **Température** : moyenne saisonnière mensuelle + **anomalie AR(1)**
  `A_t = φ·A_{t−1} + ε` (φ = persistance → spells chauds/froids) +
  **amplitude diurne** mensuelle `T_max − T_min` (requise par l'ETP de
  Hargreaves, §5.1).
- Tous les tirages viennent du **sous-flux RNG seedé `"weather"`**.

### 2.2 Scénario climat = perturbation des paramètres [DÉCISION #2]

Le scénario climat (paramètre de lancement) **perturbe les paramètres du
générateur**, pas une série figée : `ΔT` décale la moyenne saisonnière de
température, `×(1+ΔP%)` met à l'échelle l'intensité/occurrence des pluies,
optionnellement **tendanciels** sur l'horizon (réchauffement progressif).
Une seule climatologie réelle + curseurs → tous les futurs RCP.

### 2.3 Le CSV de calibration — format & dépôt

- **Format attendu** = export Météo-France « base quotidienne » que lit
  déjà `tools/extract_weather_normals.py` : séparateur `;`, colonnes
  `NUM_POSTE`, `AAAAMMJJ` (date YYYYMMDD), `RR` (pluie mm), `TM` (T°
  moyenne) — et **idéalement `TN`, `TX`** (mini/maxi, pour l'amplitude
  diurne de Hargreaves).
- **Dépôt** : `data/` à la racine du repo (versionné — la traçabilité
  *open data → normales → simulation* est un atout pour le jury).
- **Le script doit être étendu** : il sort aujourd'hui moyennes + `p_wet` +
  log-normale ; il faut ajouter les **transitions de Markov** `{P01, P11}`,
  l'**amplitude diurne** (colonnes `TN/TX`) et les **paramètres de
  température** (σ, AR(1) `φ`) que le générateur consomme.

---

## 3. Vue d'ensemble : du « modèle-étoile » au réseau

Le défaut central diagnostiqué : l'existant est une **étoile centrée sur
le profit** (le climat ne tape fort que le rendement/coût ; nappe, carbone
et biodiv sont découplés du climat). La refonte est un **réseau** où une
sécheresse se propage en cascade. Graphe cible :

```
        CSV météo (T°, pluie)        Décisions agriculteur
              │                              │
              ▼                              ▼
   ┌──────────────────────┐      ┌───────────────────────────┐
   │  ETP (Hargreaves,T°) │      │ Fert N · Couverts · Résidus│
   └──────────┬───────────┘      │ Irrigation · Flore(haies)  │
              ▼                   └───────────┬───────────────┘
        ┌───────────┐  draine        ┌────────▼─────────┐
  pluie→│ θ  RÉSERVE │───────────────▶│  N  azote sol    │
        │  EN EAU    │◀──RU_max(C)────│                  │
        │  DU SOL    │   (rétroaction)└───┬─────────┬────┘
        └─┬───┬───┬──┘                    │         │
          │   │   └───drainage──▶ h NAPPE │         │ lessivage
          │   │                           │         ▼
          │   │              ┌────────────▼───┐  (mare / aquatique)
          │   └──stress eau─▶│   Y  RENDEMENT │
          │                  └───────┬────────┘
          │                          │ résidus (ferme le cul-de-sac)
          │                          ▼
   stress │           T°(Q10) ▶┌───────────────┐
    eau   └──────────────────▶ │ C CARBONE SOL │──▶ RU_max, N_min
          │                    │  (2 pools)    │
          ▼                    └───────────────┘
   ┌──────────────┐
   │ F SANTÉ FLORE│──visuel──▶ sprite haie
   └──────┬───────┘
          ▼
   ┌──────────────┐   + chaleur, + eau, + intrants
   │ D BIODIVERSITÉ│◀───────────────────────────────
   └──────┬───────┘
          ▼
   faune visible          ┌──────────────────────────┐
                          │ ÉCONOMIE : marge, capital │
   Y, coûts, ΔC, F ──────▶│ + PAC/MAEC/PSE/crédit C   │
                          └──────────────────────────┘
```

La clé : **θ (réserve en eau du sol) est le carrefour manquant**, et
**Y→résidus→C→RU_max→θ** ferme la boucle de rétroaction qui rend une
sécheresse cumulativement destructrice.

---

## 4. Les stocks (variables d'état)

| # | Stock | Symbole | Unité | Init (param. lancement) | Bornes | Étiq. |
|---|---|---|---|---|---|---|
| 1 | Réserve en eau du sol racinaire | `θ` | mm | ~0.7·RU_max | [0, RU_max] | [NOUVEAU] |
| 2 | Profondeur de nappe | `h` | m | 2.0 | ≥ 0 | [CHANGÉ] |
| 3a | Carbone du sol — pool jeune | `C_y` | tC/ha | f(C_init) | ≥ 0 | [CHANGÉ] |
| 3b | Carbone du sol — pool vieux | `C_o` | tC/ha | f(C_init) | ≥ 0 | [CHANGÉ] |
| 4 | Azote minéral disponible | `N` | kgN/ha | ~40 | ≥ 0 | [NOUVEAU] [MODULE] |
| 5 | Rendement / biomasse culture | `Y` | t/ha | 5.5 | ≥ 0 | [CHANGÉ] |
| 6 | Santé de la flore semi-naturelle | `F` | [0,1] | f(densité haie init) | [0,1] | [NOUVEAU] |
| 7 | Biodiversité (composite) | `D` | [0,1] | 0.6 | [0,1] | [CHANGÉ] |
| 8 | Capital / marge cumulée | `K` | €/ha | 0 | libre | [GARDÉ] |

Fenêtres dérivées (pas des stocks) : jours de chaleur (T>25 °C) et de
canicule (T>30 °C) sur 30 j glissants [GARDÉ].

`C = C_y + C_o` (total carbone affiché). `RU_max` n'est pas un stock mais
un **paramètre dynamique** : `RU_max = f(C)` (§5.1).

---

## 5. Les flux (équations sourcées)

Notation : `Δx` = variation sur 1 jour (tick). Tout est intégré au pas
journalier. `clamp(x,a,b)` borne.

### 5.1 Eau du sol — bilan « bucket » FAO-56 [NOUVEAU, cœur du réalisme]

Réservoir sol type FAO-56 *(Allen et al. 1998, FAO Irrigation & Drainage
Paper 56)* :

```
ETP_0 = 0.0023 · Ra · (T_moy + 17.8) · √(T_max − T_min)      (Hargreaves 1985)
Ks    = clamp( θ / (p · RU_max), 0, 1 )           coefficient de stress hydrique
ETP_r = ETP_0 · Kc · Ks                           évapotranspiration réelle
θ'    = θ + Pluie − ETP_r
Drainage = max(0, θ' − RU_max)                    excès au-dessus de la capacité
θ_{t+1}  = clamp(θ' − Drainage, 0, RU_max)
RU_max   = RU_base · (1 + β · (C − C_ref)/C_ref)  rétroaction carbone→réserve
```

- `Ra` = rayonnement extraterrestre, fonction **calculable** de la latitude
  (~48,5°N, Perche) et du jour de l'année (équation standard FAO-56) — pas
  une donnée d'entrée.
- `Kc` = coefficient cultural saisonnier (courbe par stade ; *FAO-56*).
- `p` ≈ 0,5 = fraction d'eau facilement utilisable avant stress *(FAO-56)*.
- `RU_base` ≈ 120-150 mm (limon profond du Perche, prof. racinaire ~1 m ;
  *pédotransfert / GIS Sol — INRAE*).
- `β` (sensibilité réserve↔carbone) calibré tel que +10 g/kg de MO →
  ~+15 mm de réserve *(Hudson 1994, « Soil organic matter and available
  water capacity »)*. **C'est la rétroaction qui fait qu'un sol dégradé
  retient moins d'eau → sécheresses pires.**

**Pourquoi ça mord** : `ETP_0` monte avec `T°` (Hargreaves) → sous
réchauffement le sol s'assèche plus vite ; `Ks` chute quand `θ` baisse →
le stress se transmet *directement* au rendement, à la flore et à la
biodiversité (via `θ`). C'est le chaînon absent aujourd'hui.

### 5.2 Nappe — réservoir GARDÉNIA, alimenté par le drainage [CHANGÉ]

```
Δh = − (Drainage / 1000) / S  +  r · (h_eq − h)
```

- [CHANGÉ] La recharge vient désormais du **drainage du bucket sol**
  (couplé à `θ`), plus directement de la pluie brute.
- `S` = 0,075 (coef. emmagasinement craie) *(BRGM / SIGES Seine-Normandie)*
  [GARDÉ]. `r` = 0,012/j, `h_eq` = 3,0 m [GARDÉ] *(calibration headless)*.
- Rôle secondaire : la nappe sert le capteur piézomètre + l'accès en eau
  profonde des ligneux en saison sèche. Le **stress agronomique** passe
  désormais par `θ`, pas par `h`. [DÉCISION #3] : garde-t-on la nappe
  comme variable de premier plan, ou devient-elle un simple sous-produit
  (capteur) ? Mon avis : la garder (chaîne capteur sécheresse existante),
  mais le stress « utile » est `θ`.

### 5.3 Carbone du sol — modèle 2 pools ICBM, sensible T° et humidité [CHANGÉ]

*(Andrén & Kätterer 1997, ICBM ; modificateurs climat type RothC/AMG —
Clivot et al. 2019, AMG v2)* :

```
r_e = f_T(T) · f_θ(θ)                          facteur climat de décomposition
f_T = Q10^((T_moy − 10)/10),  Q10 ≈ 2          (Davidson & Janssens 2006)
f_θ = réponse humidité ∈ [0,1], max ~capacité au champ, faible si sec/saturé
i   = apports carbone journaliers (voir ci-dessous)
ΔC_y = i − k_y · r_e · C_y
ΔC_o = h_hum · k_y · r_e · C_y  −  k_o · r_e · C_o
```

- `k_y` ≈ 0,8/an (pool jeune), `k_o` ≈ 0,007/an (pool vieux),
  `h_hum` ≈ 0,13 (humification) *(ICBM ; ordres de grandeur AMG)*.
- **Apports `i`** [CHANGÉ — ferme le cul-de-sac économique] :
  ```
  i_an = a_résidus · résidus(Y, levier_restitution)
       + a_couverts · couverts%
       + a_flore · F                ← litière de la flore (ex-haie)
       + a_fumier · fumier
  ```
  `résidus(Y,…)` **dépend du rendement** : Y↓ (sécheresse) → moins de
  résidus → C↓. *(Apports : Solagro Afterres 2050 ; AFAC pour la flore
  ligneuse ; INRAE 4 pour 1000 pour les stocks de référence.)*

**Pourquoi ça mord** : `r_e` monte avec T° (Q10) → réchauffement =
minéralisation accélérée = `C↓` ; et `i` chute quand `Y` chute. Le
carbone, aujourd'hui inerte au climat, devient réactif.

### 5.4 Azote minéral disponible `N` — bilan complet [NOUVEAU, décision #4 verrouillée]

`N` [kgN/ha] = azote minéral du sol accessible à la culture. C'est **la
variable physique qui porte l'arbitrage éco/écolo** : elle pilote le
rendement (limitation azotée), la biodiversité (excès d'intrants), et la
qualité de l'eau (lessivage). Elle **remplace le « facteur d'intensité »
abstrait** de l'existant.

**Bilan journalier :**
```
ΔN = Apport_fert + N_min + N_dépôt + N_fix − Prélèvement − Lessivage − Pertes_gaz
```

**Entrées :**
- **`Apport_fert`** — l'azote de la fertilisation (le **levier**). Dose
  annuelle `D_N` [kgN/ha/an] **fractionnée** sur une fenêtre de printemps
  (calendrier agronomique fixe). L'azote minéral est ~immédiatement
  disponible. *(COMIFER)*
- **`N_min`** — minéralisation de la MO, **couplée au carbone** (§5.3) :
  `N_min = (k_y · r_e · C_y) / (C/N)`. `r_e = f_T(T)·f_θ(θ)` → la
  minéralisation **flambe au chaud & humide** (printemps, automne) ; le flush
  d'automne part en partie en lessivage. `C/N ≈ 10`. *(AMG / INRAE)*
- **`N_dépôt`** — dépôt atmosphérique ~constant ≈ 15 kgN/ha/an. *(EMEP/INRAE)*
- **`N_fix`** — fixation biologique, **seulement si couverts légumineux** :
  `N_fix = a_fix · couverts%_légum` (~50-150 kgN/ha). *(Justes et al.)*

**Sorties :**
- **`Prélèvement`** — absorption par la culture, **limitée par la
  disponibilité** : `Demande_N(j) = courbe de croissance × teneur N` (~22
  kgN/t de potentiel) ; `Prélèvement = min(Demande_N, N · accessibilité)`.
  Si `N` insuffisant → **stress azoté** → pénalité rendement (`Kn`, §5.5).
  *(COMIFER)*
- **`Lessivage`** — l'azote part avec le drainage (§5.1) :
  `Lessivage = λ · Drainage · (N / RU_max)`. Maximal **en automne/hiver**
  (fort drainage, faible prélèvement) → **downside qualité d'eau** (pénalité
  mare + **événement « lessivage »** pour le moteur de reco). *(INRAE)*
- **`Pertes_gaz`** — volatilisation NH₃ + dénitrification N₂O (fraction des
  apports). *(COMIFER/IPCC ; compta N₂O = GES possible post-MVP.)*

**Couplage rendement — `Kn` remplace le facteur d'intensité (§5.5) :**
`Kn = 1 − exp(−c · ΣPrélèvement / Demande_totale)` (Mitscherlich saturant).
**Rendements décroissants** : doubler `N` au-delà de l'optimum ne gagne
presque rien en rendement (plateau) mais coûte cher + lessive + abîme la
biodiv. *(Mitscherlich ; Lechenet 2017 ; COMIFER)*

**Couplage biodiversité & eau :** `inputs_factor` de la biodiv (§5.7)
**décroît** avec la dose `D_N` (eutrophisation, flore banalisée, insectes) ;
le `Lessivage` cumulé pénalise une **composante aquatique** (mare).
*(Hallmann ; Vigie-Nature)*

**Leviers qui passent par `N` :**
- **Fertilisation azotée** — fixe `D_N` ; recos **bidirectionnelles**
  (carence `Kn<0.8` → augmenter ; excès/lessivage → baisser).
- **Couverts d'interculture** — `N_fix` (si légumineux) **+ piège à
  nitrates** (capte l'azote résiduel d'automne → **réduit le lessivage**).
- **Travail du sol réduit** — **immobilisation transitoire** (résidus de
  surface, C/N élevé, bloquent `N` quelques semaines) = son downside azoté.

**L'optimum intérieur (« optimiser, pas moraliser ») :** la dose qui
maximise l'objectif (marge − risque + écologie monétisée) est **en dessous**
de la dose qui maximise le rendement — au-delà, chaque kgN coûte (intrant +
lessivage + biodiv + perte de MAEC) plus qu'il ne rapporte. **Baisser
l'azote peut être rentable**, jusqu'à un point. L'optimiseur (C.4 du doc 10)
le *révèle*, ne l'impose pas.

**Paramètres :**
| Symbole | Sens | Valeur | Source | Conf. |
|---|---|---|---|---|
| `C/N` | rapport C/N humus | 10 | sols tempérés | ⬤ |
| `N_dépôt` | dépôt atmosphérique | ~15 kgN/ha/an | EMEP/INRAE | ◐ |
| `a_fix` | fixation couvert légumineux | 50-150 kgN/ha | Justes et al. | ◐ |
| teneur N | azote / rendement | ~22 kgN/t | COMIFER | ⬤ |
| `λ` | fraction lessivable | à calibrer | COMIFER | ○ |
| `c` | courbure Mitscherlich N | à calibrer | Lechenet | ○ |
| frac. volatilisation | pertes gazeuses | ~10 % apports | IPCC/COMIFER | ◐ |

### 5.5 Rendement [CHANGÉ]

```
Y_cible = Y_pot · Ks_saison · Kn · K_chaleur · K_intensité
```

- `Ks_saison` = intégrale saisonnière du stress hydrique journalier `Ks`
  *(réponse rendement-eau, Doorenbos & Kassam 1979, FAO-33, coef. Ky)*.
  **[NOUVEAU couplage]** : la sécheresse agit enfin sur le rendement via
  l'eau du sol réelle.
- `Kn` = réponse azotée saturante (Mitscherlich, §5.4) *(COMIFER)* —
  **remplace** le `K_intensité` abstrait (décision #4 verrouillée).
- `K_chaleur` = pénalité chaleur 6 %/°C + stress aigu jours >25 °C [GARDÉ]
  *(IPCC AR6 ch.5)*.
- `Y_pot` = 5,5 t/ha (blé/colza Eure-et-Loir/Orne) [GARDÉ] *(Agreste)*.
- Relaxation EMA vers la cible (constante ~saison). Y alimente les résidus
  (→ carbone) et la marge.

### 5.6 Santé de la flore `F` (ex-haie) [NOUVEAU — reframe]

```
F_cible = f_eau(θ_saison) · f_intrants(N) · f_gestion(plantation, arrachage)
```

- Baisse sous stress hydrique (θ bas), sous forte charge d'intrants (la
  flore sauvage régresse), sous arrachage. Monte par plantation / réduction
  d'arrachage / baisse d'intrants.
- **`F` ne pilote rien d'économique.** Elle (a) est **rendue visible par le
  sprite de haie** (haie dense/verte = flore saine), (b) alimente le facteur
  habitat de la biodiversité, (c) fournit de la litière au carbone.
- EMA lente (mois-années). *(AFAC-Agroforesteries ; Réseau Haies ;
  Constant et al. 1976 pour le lien flore ligneuse↔passereaux.)*

### 5.7 Biodiversité `D` [CHANGÉ — couplée au climat]

```
D_cible = w_h·habitat(F) + w_w·eau(θ, mare) + w_i·intrants(N) + climat(chaleur)
D ← D + (1/τ)·(D_cible − D)                         τ ≈ 1 an
```

- [CHANGÉ] Le **terme eau suit `θ`** (stress hydrique réel), plus la nappe
  plate. Ajout d'un **terme chaleur** (au-delà du seul plafond canicule).
  Terme intrants piloté par `N` (et/ou proxy pesticides).
- Poids `w_h, w_w, w_i` *(Hallmann 2017 ; Vigie-Nature/INRAE-OFB ; MNHN 2024)*.
- [MODULE optionnel] décomposition en guildes (oiseaux = habitat ;
  insectes = intrants + chaleur ; aquatique = lessivage N + mare). Réalisme
  élevé, coût moyen. [DÉCISION #5].
- `D` pilote la faune visible [GARDÉ].

### 5.8 Économie [GARDÉ, étendu]

```
Marge_an = Y · prix − coût_N(Fert) − coût_méca/carburant − coût_irrigation
         + PAC_base + MAEC + PSE + crédit_carbone(ΔC vs baseline)
K ← K + Marge_an/365 − investissements
Risque = écart-type inter-annuel de la marge (via rejeu de plusieurs années)
```

- Prix 250 €/t, PAC base 220 €/ha, bonus/PSE [GARDÉ] *(Agreste, PAC 2025,
  CIVAM)*. [CHANGÉ] : coût d'entretien de haie retiré du moteur éco (la
  haie n'est plus un poste de rentabilité) — [DÉCISION #1 liée].
- **[NOUVEAU] crédit carbone** : `ΔC` au-dessus de la baseline monétisé
  *(Label Bas-Carbone — méthodes grandes cultures / haies)*. C'est la
  monétisation des services écosystémiques au cœur de la thèse.

---

## 6. Les couplages : la cascade sécheresse, pas à pas

Sous **−50 % de pluie / +ΔT sur 10 ans**, voici la chaîne qui *manquait*
et qui fonctionne maintenant (chaque flèche = une équation ci-dessus) :

```
Pluie↓ (CSV transformé) ┐
T°↑ → ETP_0↑ ───────────┤→ θ↓ (réserve en eau du sol s'effondre)
                         │
   θ↓ → Ks↓ ────────────┼→ Y↓ (rendement, stress hydrique réel)
                         ├→ F↓ (flore stressée → haie visiblement dégradée)
                         └→ D↓ (terme eau de la biodiversité)
   Y↓ → résidus↓ ────────→ C↓ (apports carbone chutent)
   T°↑ → r_e↑ (Q10) ─────→ C↓ (minéralisation accélérée)
   C↓ → RU_max↓ ─────────→ θ↓ ENCORE  ◀── BOUCLE DE RÉTROACTION
   C↓ → N_min↓ ──────────→ N↓ → Y↓
   F↓ → habitat↓ ────────→ D↓
   T°↑ (canicule) ───────→ D↓ (insectes)
   Y↓, irrigation↑ ──────→ Marge↓↓
```

**Résultat attendu** : marge ↓↓, **biodiversité ↓, carbone ↓, réserve en
eau visiblement basse, flore/haie dégradée**. Plus une étoile : un système
qui se dégrade de façon cohérente et cumulative. C'est exactement ce qui
manquait au test « 10 ans à −50 % et tout va bien sauf l'éco ».

---

## 7. Les leviers agricoles (réalistes, mécanistes)

Tous **directement actionnables** (sliders/boutons), conformément à la
règle reco ⊆ leviers. Effet **mécaniste** (sur un flux), jamais un
coefficient figé.

| Levier | Agit sur | Effet mécaniste | Coût | Source |
|---|---|---|---|---|
| **Fertilisation azotée** | `Fert` → N | Y↑ (Mitscherlich, saturant) ; D↓ ; lessivage↑ | coût intrants↑ | COMIFER ; Vigie-Nature |
| **Couverts d'interculture** | apports C ; N_fix ; évaporation↓ (θ) ; lessivage↓ | C↑ ; N↑ ; θ mieux retenu ; eau qualité↑ | semence/méca | Solagro ; Justes et al. |
| **Restitution des résidus** | apports C (∝ Y) | C↑ (lentement) | manque à gagner paille | Solagro ; INRAE 4‰ |
| **Irrigation** | `θ` direct | θ↑ → lève le stress ; sous contrainte de ressource (nappe/quota) | coût eau/énergie | FAO-56 |
| **Plantation / gestion flore (haies)** | `F` | F↑ → habitat → D↑ ; litière → C | coût plantation (one-shot) | AFAC ; Réseau Haies |
| **[MODULE] Mare / bandes enherbées** | lessivage↓ ; habitat aquatique | D aquatique↑ | aménagement | Efese ; PNR Perche |

Réglages : magnitude par défaut = **l'optimum calculé** (voir §9), pas un
pas fixe. Transitions douces 7-14 j [GARDÉ].

---

## 8. Les KPI (clairs, priorité agriculteur)

5 Hero KPI, ordonnés par priorité réelle d'un agriculteur *(Edwards-Jones
2006 ; Reimer et al. 2012)* :

| # | Hero KPI | Unité | Pourquoi c'est une priorité |
|---|---|---|---|
| 1 | **Marge / rentabilité** | €/ha/an | Priorité n°1, survie économique |
| 2 | **Rendement & sa stabilité (risque)** | t/ha + σ | Production + exposition à l'aléa |
| 3 | **Biodiversité** | indice [0,1] | Service écosystémique + conditionnalité aides |
| 4 | **Carbone du sol** | tC/ha | Fertilité long terme + **monétisable** (crédit) |
| 5 | **Réserve en eau / stress hydrique** | % RU | Résilience sécheresse, devient critique sous climat |

+ **Apport de la techno** (réel vs fantôme) [GARDÉ] — la valeur ajoutée de
l'instrumentation/décision, NET d'investissement.

**La haie** [DÉCISION #1] : aujourd'hui « densité de haie » est un Hero KPI.
Or la haie n'est qu'un **proxy visuel de `F`**. Options :
- (a) remplacer le KPI « densité de haie » par **« santé de la flore /
  habitat »** (= `F`, plus honnête) ;
- (b) garder « densité de haie » comme chiffre familier, **dérivé de `F`** ;
- (c) fondre la flore dans le KPI biodiversité (un Hero KPI de moins).
Mon avis : **(a)** — nomme la variable réelle, garde la haie comme son
incarnation à l'écran.

---

## 9. Le modèle de décision (rationnel, impactant, expliqué)

Architecture **conservée** [GARDÉ] : détection d'événement (capteur) →
recommandation → projection forward réelle (`ModelOutcomeProjector`) →
journal. Ce qu'on **change** :

### 9.1 Objectif fondé [CHANGÉ — fini le 0,8/0,2 arbitraire]

L'agriculteur maximise une **marge actualisée ajustée du risque**, où
l'écologie entre par deux portes *concrètes* (plus par un poids inventé) :

```
U(levier) = NPV(marge sur horizon)  −  λ · σ(marge)  +  services monétisés
```

- **Monétisation** : MAEC, PSE, crédit carbone (`ΔC`) entrent *déjà dans la
  marge* → la biodiversité/carbone pèsent en **euros réels**, traçables.
- **Résilience** : un sol/une réserve en eau tamponnés **réduisent `σ`**
  (variance de marge sous aléa météo) → l'écologie « paie » via la
  stabilité, mesurable par rejeu de plusieurs années météo.
- `λ` = aversion au risque *(littérature agro-éco ; calibrable)*.

### 9.2 Recommandations **impactantes** [CHANGÉ — corrige ton P1]

Défaut actuel : « baisser les intrants » par pas de 0,2 qui s'enchaînent,
sans jamais donner le **taux optimal**. Correction :

> Le moteur **balaie le niveau du levier** (ex. dose d'azote de 0 à max),
> projette `U` pour chacun, et recommande **d'atteindre directement le
> niveau qui maximise `U`** — en une décision informée, pas en clics
> répétés. Le slider s'ouvre **par défaut sur cet optimum**, l'utilisateur
> peut s'en écarter.

### 9.3 Recommandations **expliquées** [CHANGÉ]

Chaque popup porte :
1. **Provenance** : capteur → événement → mécanisme (« piézomètre : nappe
   > 2,6 m depuis 34 j → sécheresse → la réserve en eau du sol est à 18 %
   de la RU »).
2. **Effet projeté** : ΔKPI par horizon **avec bande d'incertitude** issue
   du **rejeu de plusieurs années météo réelles** (worst/expected/best),
   pas d'un bruit synthétique.
3. **Coût** + **horizon de rentabilité**.
4. **Classification** WinWin / compromis éco / compromis écolo / perdant-
   perdant [GARDÉ].

### 9.4 Couverture & rationalité [GARDÉ, affiné]

Une reco ne se déclenche que si son `ΔU` est positif et que le levier est
**faisable** (gardes de borne). Les événements déclinés sont marqués
(pas de re-proposition à chaque tick) [GARDÉ].

---

## 10. Tableau de paramètres (sourcé)

`confiance` : ⬤ solide (source primaire) · ◐ plausible (ordre de grandeur) · ○ à calibrer (headless).

| Symbole | Sens | Valeur | Unité | Source | Conf. |
|---|---|---|---|---|---|
| `Ra` | rayonnement extraterrestre | calc(48,5°N, jour) | MJ/m²/j | FAO-56 | ⬤ |
| `Kc` | coef. cultural | 0,4–1,15 (courbe) | – | FAO-56 | ⬤ |
| `p` | fraction eau facilement utilisable | 0,5 | – | FAO-56 | ⬤ |
| `RU_base` | réserve utile de base | 130 | mm | INRAE GIS Sol / pédotransfert | ◐ |
| `β` | sensibilité RU↔carbone | t.q. +10 g/kg→+15 mm | – | Hudson 1994 | ◐ |
| `S` | emmagasinement nappe | 0,075 | – | BRGM SIGES | ⬤ |
| `r`,`h_eq` | récession, équilibre nappe | 0,012/j ; 3,0 m | – | calib. headless | ○ |
| `Q10` | sensibilité T° minéralisation | 2,0 | – | Davidson & Janssens 2006 | ⬤ |
| `k_y`,`k_o` | décroissance C jeune/vieux | 0,8 ; 0,007 | /an | ICBM (Andrén & Kätterer 1997) | ⬤ |
| `h_hum` | humification | 0,13 | – | ICBM / AMG | ⬤ |
| `a_résidus…` | coefs apports carbone | 0,8 / 1,2 / 0,4 | tC/ha/an | Solagro ; AFAC | ◐ |
| `C/N` | rapport C/N du sol | 10 | – | sols tempérés | ⬤ |
| `λ_lessiv` | fraction N lessivable | à calibrer | – | COMIFER | ○ |
| `Ky` | réponse rendement-eau | par stade | – | Doorenbos & Kassam 1979 | ⬤ |
| `Y_pot` | rendement potentiel | 5,5 | t/ha | Agreste 2015-24 | ⬤ |
| pénalité chaleur | sur rendement | 6 %/°C | – | IPCC AR6 ch.5 | ⬤ |
| prix culture | farm-gate | 250 | €/t | Eure-et-Loir 2022 | ⬤ |
| PAC base / bonus | paiements | 220 / 20 | €/ha | PAC 2025 | ⬤ |
| coût intrants base | référence | 1200 | €/ha/an | CIVAM / AFPF | ◐ |
| seuils biodiv `w_*` | poids habitat/eau/intrants | 0,40/0,25/0,35 | – | Hallmann ; Vigie-Nature | ◐ |
| `λ` risque | aversion au risque | à calibrer | – | litt. agro-éco | ○ |
| crédit carbone | valeur tCO₂ | ~30–40 | €/tCO₂ | Label Bas-Carbone | ◐ |

*(Sources déjà présentes et réutilisées de l'existant : Météo-France
normales Mortagne, BRGM GARDÉNIA, INRAE 4‰/BDAT, Solagro Afterres 2050,
AFAC, Hallmann 2017, MNHN 2024, Vigie-Nature, FluxNet, Edwards-Jones,
Reimer.)*

---

## 11. Batterie de comportements attendus (cibles de conception = futurs tests)

Ces assertions sont **à la fois** le cahier des charges du réalisme **et**
le banc de tests headless de la §12. Le modèle DOIT produire :

| # | Scénario | Comportement attendu |
|---|---|---|
| B1 | Neutre (météo réelle, 0 décision), 10 ans | tous KPI stables ± variabilité inter-annuelle |
| B2 | −50 % pluie + 3 °C, 10 ans, 0 décision | **rendement ↓↓, biodiv ↓, carbone ↓, réserve en eau basse** (plus de « tout va bien ») |
| B3 | Sécheresse soutenue | la chaîne capteur (piézo/réserve) déclenche bien l'alerte → reco irrigation/couverts |
| B4 | Forte fertilisation azotée | rendement ↑ mais **biodiv ↓ et lessivage ↑** (arbitrage visible) |
| B5 | Couverts + résidus, 10 ans | carbone ↑, RU_max ↑ → **meilleure résilience** (σ marge ↓ sous aléa) |
| B6 | Plantation flore/haies | F ↑ → habitat → biodiv ↑ sur quelques années ; **0 effet sur la marge** (haie non-moteur) |
| B7 | Déterminisme | même seed + mêmes entrées (CSV, décisions) → état identique |
| B8 | Run fantôme | mêmes entrées → divergence **uniquement** due aux décisions |
| B9 | Conservation | bilans (eau, carbone, azote) bouclent ; aucun stock négatif ou divergent |
| B10 | Reco optimale | la dose recommandée maximise bien `U` (pas un pas arbitraire) |

---

## 12. Plan de vérification (avant toute ligne de code… puis pendant)

**Gate scientifique** : chaque paramètre du §10 pointe vers une source ou
est marqué `○ à calibrer`. Aucune valeur orpheline.

**Gate mathématique** (via le harnais headless dotnet, couches 01-04) :
- **Analyse dimensionnelle** : unités cohérentes sur chaque équation.
- **Conservation** : bilans eau/carbone/azote bouclent (entrées − sorties =
  Δstock), à epsilon près.
- **Stabilité** : pas de stock qui diverge ; bornes respectées ; pas
  d'oscillation numérique (pas de temps adapté).
- **Équilibres** : les états stationnaires correspondent à des valeurs de
  référence réelles (ex. `C_eq` ≈ stock BDAT du Perche ; `θ` moyen ≈ bilan
  hydrique climatique régional).
- **Signes de sensibilité** : chaque dérivée partielle a le bon signe
  (∂Y/∂θ > 0, ∂C/∂T < 0, ∂D/∂N < 0, …) — c'est la batterie B1-B10.

Le modèle n'est « prêt à coder en Unity » que quand B1-B10 passent en
headless.

---

## 13. Décisions (verrouillées le 2026-06-08)

| # | Décision | Choix verrouillé |
|---|---|---|
| **1** | Haie : statut KPI + couplages | **Garder « densité de haie »** comme KPI ET variable d'état — elle porte le rôle « santé de la flore » (pilotée par eau `θ` / intrants `N` / gestion). Haie **retirée du moteur économique** : ni rendement, ni coût d'entretien. Reste : habitat biodiv + litière carbone + proxy visuel. *(On fond donc `F` dans `HedgerowDensity` : une seule variable, unité m/ha familière, normalisée [0,1] là où la biodiv/le carbone la consomment.)* |
| **2** | Météo : génération, pas rejeu | **Générateur stochastique seedé calibré sur les stats du CSV** (Markov occurrence + AR(1) température → persistance réelle ; pas de rejeu d'année en boucle). Le scénario climat perturbe les **paramètres** du générateur (`ΔT`, `×pluie`), tendanciels sur l'horizon. CSV = source de calibration (format MF), déposé dans `data/`. |
| **3** | Nappe `h` | **Gardée** (chaîne capteur piézomètre) ; le stress agronomique « utile » passe par `θ`. |
| **4** | Azote | **Module explicite** (§5.4) — cœur de l'arbitrage éco/écolo. Remplace le facteur d'intensité abstrait. |
| **5** | Biodiversité | **Composite d'abord** (§5.7) ; guildes oiseaux/insectes/aquatique en option ultérieure. |
| **6** | Emplacement final du doc | **Repli dans `docs/`** du repo une fois la spec validée. |

---

## 14. Modules & dosage du scope

Pour respecter ta règle 5 (« complexité OK si elle sert le réalisme ») tout
en gardant la main sur le budget :

| Brique | Réalisme apporté | Coût | Reco |
|---|---|---|---|
| Bucket eau du sol FAO-56 + Hargreaves | **décisif** (débloque toute la cascade) | moyen | **indispensable** |
| Rétroaction carbone→RU_max | élevé (sécheresses cumulatives) | faible | **oui** |
| Carbone 2 pools + Q10 | élevé | faible | **oui** |
| Météo par CSV rejoué | élevé (persistance, run fantôme propre) | faible | **oui** |
| Module azote explicite | élevé (arbitrage éco/écolo physique) | moyen | **oui** (décision #4) |
| Objectif marge-risque-monétisation | élevé (décisions rationnelles) | moyen | **oui** |
| Guildes de biodiversité | moyen | moyen | option |
| Mare / bandes enherbées | moyen | moyen | option |

---

## 15. Prochaine étape

Tu lis, tu réagis (surtout §13). Dès que le cadre est validé, on descend
étape par étape (stocks → flux → couplages → leviers → KPI → objectif →
paramètres → batterie), chacune vérifiée en headless, **avant** de toucher
au code Unity.
