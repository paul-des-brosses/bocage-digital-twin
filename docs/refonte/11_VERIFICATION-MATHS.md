# Refonte backend — 11 · Gate de vérification mathématique (spec cible)

> **Statut** : SPEC AUTORITAIRE de la refonte. Le **« gate papier »** :
> vérification analytique que le modèle (08 + 10) tient — unités, équilibres,
> stabilité, signes — **avant d'écrire une ligne**.
> **Certifie** : cohérence mathématique + comportements de bon ordre de
> grandeur. **Ne remplace pas** : le **gate numérique** (batterie B1-B10 de
> 08 §11) qui tournera dans le harnais headless une fois le modèle codé.

Notation : `x*` = valeur d'équilibre ; toutes les constantes de taux sont
exprimées **par an** dans la calibration et converties **par jour** (÷365)
à l'intégration (piège d'implémentation n°1).

---

## 1. Analyse dimensionnelle

Chaque équation est intégrée au pas journalier : `x_{t+1} = x_t + (taux)·1 jour`.
On vérifie que tous les termes d'une même somme ont la **même unité**.

| Équation | Terme | Unité | OK |
|---|---|---|---|
| Eau `θ` | Pluie, ETP_r, Drainage | mm/j | ✅ |
| | `RU_max = RU_base(1+β·(C−C_ref)/C_ref)` | mm (β sans dim.) | ✅ |
| ETP Hargreaves | `0.0023·Ra·(T+17.8)·√(ΔT)` | mm/j **ssi Ra en mm/j équiv.** | ⚠️ |
| Nappe `h` | `(Drainage/1000)/S` ; `r·(h_eq−h)` | m/j | ✅ |
| Carbone `C_y,C_o` | `i` ; `k·r_e·C` | tC/ha/j (k en /j) | ✅ |
| Azote `N` | `N_min=(k_y·r_e·C_y)/(C/N)·1000` | kgN/ha/j | ✅ |
| | `Lessivage=λ·Drainage·(N/RU_max)` | (sans dim.)·(mm/j)·(kgN/ha/mm) = kgN/ha/j | ✅ |
| Rendement `Y` | `(1/τ_Y)(Y_cible−Y)` | t/ha/j | ✅ |
| Biodiv `D`, densité, `W` | `(1/τ)(cible−x)` | (sans dim.)/j | ✅ |

**Deux pièges d'unités à coder soigneusement :**
1. **`Ra` doit être en mm/j d'évaporation équivalente** (MJ/m²/j ÷ 2,45),
   sinon Hargreaves sort un nombre ~×2,45 trop grand. *(FAO-56)*
2. **Constantes de taux `/an` → `/jour`** (`k_y, k_o, r, …`). Oublier le ÷365
   est l'erreur classique.

→ **Dimensionnellement cohérent**, sous réserve des deux conversions ci-dessus.

---

## 2. Analyse des équilibres

### 2.1 Carbone — le test le plus parlant

À l'état stationnaire (`ΔC_y = ΔC_o = 0`) le système ICBM 2 pools donne :

```
C_y* = i / (k_y·r_e)
C_o* = h_hum·i / (k_o·r_e)
C*   = C_y* + C_o* = (i / r_e) · (1/k_y + h_hum/k_o)
```

Avec `k_y=0,8/an`, `k_o=0,007/an`, `h_hum=0,13`, `r_e≈1` (T,θ de référence) :

```
1/k_y = 1,25      h_hum/k_o = 18,57      somme = 19,82 an
C* = i · 19,82
```

- **Pour `C* = 50 tC/ha`** (référence BDAT Perche) → **`i ≈ 2,5 tC/ha/an`**.
  C'est un apport carbone (résidus + couverts + flore + retour MO) tout à
  fait **plausible** (fourchette réelle 1,5-4) → **le couple (paramètres
  ICBM, apports réalistes) tombe sur le bon stock. ✅**
- **Sensibilité au réchauffement** : `C* ∝ 1/r_e`. À +3 °C, `r_e` passe de
  ~1,07 à ~1,32 (Q10=2) → **`C*` chute de ~20 %** (50 → ~41) rien que par la
  température. Combiné à la baisse d'apports sous sécheresse (`Y↓ → i↓`), la
  chute est plus forte. **C'est exactement le couplage carbone↔climat absent
  de l'ancien modèle. ✅**

> **Caveat honnête (calibration)** : le pool *vieux* domine (`C_o*≈46`) avec
> une constante de temps `1/k_o ≈ 140 ans`. Sur 10 ans, le carbone **bouge
> donc lentement** (réaliste : le SOC est « collant »). Le signal décennal
> vient surtout du pool *jeune* (`τ≈1,25 an`, mais petit ~3 tC/ha) et de la
> dérive du couplage T°/apports. **Conséquence** : la correction du défaut
> « 10 ans et le carbone ne bouge pas » se fait surtout via `θ/rendement/
> biodiv` (rapides et visibles) **+** un carbone qui va dans le bon sens
> (lent mais réel). Ne pas attendre de grands swings de carbone — ce serait
> irréaliste.

### 2.2 Nappe

`Δh=0` → `h* = h_eq − Drainage/(1000·S·r)`. Avec `S=0,075`, `r=0,012/j` :

- Drainage moyen ~150 mm/an (0,41 mm/j) → `h_eq − h* ≈ 0,46 m` → **`h* ≈ 2,5 m`**.
- En recharge hivernale (drainage 1-2 mm/j) → `h*` remonte vers ~1,3-2 m.
- Sous sécheresse (drainage→0) → `h* → h_eq = 3,0 m`.

→ **`h` cycle entre ~1,3 m (hiver) et ~3 m (été)** : nappe peu profonde de
bocage, **structurellement bornée près de 3 m**. ✅ Ceci **re-confirme la
décision** de faire passer le stress agronomique par `θ` (la nappe ne peut
pas, à elle seule, « entrer en crise »).

### 2.3 Eau du sol `θ`, azote `N`

- **`θ`** n'a **pas d'équilibre statique** (forçage météo journalier) : il
  cycle (été : `ETP>Pluie` → `θ↓` vers la zone de stress ; hiver : remplit
  `RU_max`, l'excès draine). **Borné `[0, RU_max]` par construction** (le
  `clamp`). Bilan annuel : `Σpluie ≈ ΣETP_r + Σdrainage` (~800 ≈ ~650 + ~150
  mm) → **bilan régional cohérent. ✅**
- **`N`** : turnover rapide (prélèvement + lessivage ≫ stock), fortement
  forcé (apports, croissance, lessivage hivernal). Pas d'équilibre statique,
  mais **bilan annuel bouclé** et **pool borné** (~20-50 kgN/ha plausible). ✅

### 2.4 Rendement, biodiversité, densité, adventices

Tous en **relaxation EMA** vers une cible bornée → ils convergent vers leur
cible et **héritent de ses bornes**. Pas d'équilibre « libre » à vérifier. ✅

---

## 3. Stabilité

| État | Nature | Verdict |
|---|---|---|
| `C_y, C_o` | linéaire, valeurs propres `−k_y·r_e`, `−k_o·r_e` (< 0) | **stable** (approche monotone) ✅ |
| `θ` | bucket borné `[0,RU_max]`, contraction (ETP+drainage évacuent l'excès) | **stable / borné** ✅ |
| `h` | relaxation linéaire, valeur propre `−r < 0` | **stable** ✅ |
| `N` | bornes + feedback négatif (pertes ∝ `N`), `λ·Drainage/RU_max < 1`/j | **stable** ✅ |
| `Y, D, densité, W` | EMA contractive (`1/τ ∈ (0,1)` ⇔ `τ ≥ 1 j`) | **stable / borné** ✅ |

**Le seul point à surveiller — la boucle `C ↔ θ` (via `RU_max`)** :
`C → RU_max → θ → {f_θ→r_e ; Y→i} → C`. Elle mêle un feedback **négatif**
(`θ↑ → r_e↑ → minéralisation↑ → C↓`) et **positif** (`θ↑ → Y↑ → i↑ → C↑`).

- Gain de boucle `∝ β` (sensibilité `RU_max↔C`). À `β≈0,5` (±30 % C → ±15 %
  `RU_max`), `dRU_max/dC ≈ 1,3 mm/(tC/ha)`, et les sensibilités aval (`dθ/
  dRU_max < 1`, `di/dθ`, `dr_e/dθ` modestes) → **gain ≪ 1 → contractif**.
- En plus, la boucle évolue sur le **temps long du carbone** (siècles pour
  le vieux pool) → **aucun emballement rapide possible**.

→ **Condition à garder** : borner `β` (gain ∝ β). À `β` réaliste, stable.
**À confirmer numériquement** au gate B1-B10 (test B9).

---

## 4. Signes de sensibilité (chaque arête causale)

On vérifie que chaque dérivée partielle a le **signe du monde réel** :

| Dérivée | Signe attendu | Mécanisme | OK |
|---|---|---|---|
| `∂θ/∂Pluie` | + | recharge | ✅ |
| `∂ETP_0/∂T` | + | Hargreaves | ✅ |
| `∂θ/∂T` | − | `ETP↑ → θ↓` | ✅ |
| `∂Y/∂θ` | + | stress hydrique levé (`Ks`) | ✅ |
| `∂Y/∂N` | + mais **saturant** (`∂²Y/∂N²<0`) | Mitscherlich | ✅ |
| `∂Y/∂T` | − | pénalité chaleur | ✅ |
| `∂Y/∂W` | − | adventices | ✅ |
| `∂C*/∂i` | + | plus d'apports | ✅ |
| `∂C*/∂T` | − | `r_e↑ → minéralisation↑` | ✅ |
| `∂RU_max/∂C` | + | MO → réserve utile (β>0) | ✅ |
| `∂θ/∂C` | + | **boucle résilience** | ✅ |
| `∂N_min/∂T` | + | `r_e` | ✅ |
| `∂Lessivage/∂Drainage` | + | transport | ✅ |
| `∂Lessivage/∂N` | + | concentration | ✅ |
| `∂biodiv/∂N` | − | eutrophisation/intrants | ✅ |
| `∂biodiv/∂θ` | + | facteur eau | ✅ |
| `∂biodiv/∂densité` | + | habitat | ✅ |
| `∂biodiv/∂T` | − | canicule | ✅ |
| `∂W/∂(réduction travail)` | + | moins de destruction mécanique | ✅ |

→ **Tous les signes sont corrects.** Aucune arête « à l'envers ».

---

## 5. Existence d'un optimum intérieur (« optimiser, pas moraliser », prouvé)

C'est la propriété mathématique qui garantit que le DT propose des
**arbitrages**, pas des solutions de coin.

**Azote.** La marge en fonction de la dose `N` :
```
Marge(N) = prix · Y(N) − c_N · N + (constantes)
```
`Y(N)` est **concave** (Mitscherlich, `Y'>0`, `Y''<0`). Donc `Marge(N)` est
concave → **maximum intérieur unique `N*`** défini par :
```
prix · Y'(N*) = c_N      (valeur marginale du rendement = coût marginal de l'azote)
```
Comme `Y'` décroît d'une grande valeur vers ~0, il existe un `N*` fini, **et
il est sous la dose qui maximise le rendement** (où `Y'=0`). Ajouter le
lessivage/biodiv (coûts croissants en `N`) et les paiements MAEC (qui
récompensent `N` bas) **abaisse encore `N*`**. → **Baisser l'azote est
rentable jusqu'à `N*`. Démontré, pas imposé. ✅**

**Travail du sol.** Bénéfice (carbone + eau + carburant) **concave** vs coût
(adventices → pesticide/rendement) **convexe** → **intensité de travail
optimale intérieure** (typiquement TCS, pas semis direct intégral). ✅

→ Le moteur de reco (C.4 du doc 10) **cherche ces optima** ; le modèle
**garantit qu'ils existent et sont intérieurs**.

---

## 6. Conservation

- **Eau** : `Δθ = Pluie − ETP_r − Drainage` ; sur l'année `Σentrées =
  Σsorties + Δθ_annuel ≈ 0`. **Bilan bouclé** par construction. ✅
- **Carbone** : `ΔC_total = i − Respiration`, avec
  `Respiration = (1−h_hum)·k_y·r_e·C_y + k_o·r_e·C_o`. Le carbone **perdu**
  l'est vers l'atmosphère (= émission CO₂) — c'est correct, pas une fuite de
  bilan. **Cohérence capteur** : le flux mesuré par la tour Eddy (NEE) doit
  égaler `Respiration − i`. ✅
- **Azote** : `ΔN = entrées − sorties`, toutes comptées ; la fixation vient
  de l'air, les pertes gazeuses y retournent. **Bilan bouclé.** ✅

---

## 7. Verdict & pièges d'implémentation

**Verdict : le modèle passe le gate papier.** ✅
- Dimensionnellement cohérent.
- Équilibres réalistes : `C*≈50 tC/ha` (BDAT), `h*∈[1,3 ; 3,0] m`, bilans
  eau/azote bouclés.
- Stable (le seul point de vigilance, la boucle `C↔θ`, est contractif à `β`
  réaliste).
- Tous les signes de sensibilité corrects.
- Optima intérieurs **démontrés** (azote, travail du sol).
- Conservation eau/carbone/azote bouclée.

**Pièges à coder soigneusement** (sinon on casse une propriété vérifiée) :
1. `Ra` en mm/j équivalent (÷2,45) dans Hargreaves.
2. Constantes de taux `/an → /jour` (÷365).
3. **Ordre des termes azote** : prélèvement **avant** lessivage (le lessivage
   porte sur le `N` *restant*), sinon double ponction.
4. Borner `β` (gain de la boucle `C↔θ`).
5. `Ks`, `f_θ` bien **clampés [0,1]**.

**Caveat de calibration** (à garder en tête) : le carbone bouge **lentement**
(réaliste) — le signal décennal visible est porté par `θ/rendement/biodiv`.

---

## 8. Ce qui passe au gate numérique (headless)

Le gate papier ne peut pas tout : ces points se confirment **en chiffres**
une fois le modèle codé dans le harnais (couches 01-04), via la batterie
B1-B10 de 08 §11 :
- B2 : `−50 % pluie / 10 ans` → décroissance effective de rendement, biodiv,
  carbone (amplitudes réelles).
- B9 : conservation numérique + **stabilité de la boucle `C↔θ`** (pas de
  divergence).
- B10 : la dose recommandée maximise bien `U` (l'optimum intérieur calculé).

→ **Le modèle est sain sur le papier. La prochaine phase est l'implémentation
headless** (et c'est là que commence le code des couches 01-04).
