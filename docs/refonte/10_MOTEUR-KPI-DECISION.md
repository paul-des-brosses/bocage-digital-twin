# Refonte backend — 10 · Cœur de simulation, KPI, moteur de recommandation (spec cible)

> **Statut** : SPEC AUTORITAIRE de la refonte. Compagnon de **08** (08 =
> quoi : stocks, flux, sources ; **10 = comment** : la boucle qui fait
> tourner le modèle, calcule les KPI, fabrique les recommandations). Voir
> aussi **11** (vérification maths).
> Décisions §13 de 08 verrouillées (densité haie = proxy flore sans rôle
> éco ; azote explicite ; météo générée depuis le CSV ; nappe secondaire,
> stress utile via `θ` ; biodiv composite).

---

## 0. Mes recommandations de conception (le digest)

Avant le détail, les 10 partis-pris que je recommande, et qui structurent
tout ce document :

1. **`θ` (réserve en eau du sol) est le carrefour.** Tout le réalisme
   sécheresse passe par lui. Bucket FAO-56, évapotranspiration de
   Hargreaves (sensible à la T°).
2. **Météo générée par un générateur seedé calibré sur les stats du CSV**
   (pas de rejeu en boucle). Persistance réelle (Markov + AR(1)),
   déterministe → run fantôme exact.
3. **Carbone 2 pools + Q10** : le sol réagit enfin à la chaleur.
4. **Azote explicite** : la variable physique qui porte l'arbitrage
   éco/écolo (rendement vs biodiv vs lessivage).
5. **Densité de haie = proxy de la flore**, hors moteur économique.
6. **L'humidité est mesurée par la station météo** (pas de capteur dédié au
   MVP) : c'est elle qui arme l'alerte stress hydrique (primauté du capteur).
7. **Objectif de décision fondé** : marge actualisée **ajustée du risque**,
   écologie monétisée (PSE/MAEC/crédit carbone) — fini le 0,8/0,2.
8. **Recommandations à dose optimale** : le moteur cherche le *niveau* de
   levier qui maximise l'objectif, et le recommande en une fois (corrige
   ton P1 « −0,2 en boucle »).
9. **Événements azote bidirectionnels** : carence → « augmenter N » ;
   excès/lessivage → « baisser N ». Plus de recos, mieux déclenchées.
10. **Bande d'incertitude = plusieurs réalisations seedées du générateur**
    (variabilité inter-annuelle réaliste), pas un bruit synthétique.

---

# PARTIE A — Le cœur de la simulation

## A.1 Les trois entrées et leur rôle exact

| Entrée | Contenu | Quand elle agit |
|---|---|---|
| **`SimulationConfig`** (params de lancement) | stocks initiaux (`θ₀, C₀, N₀, densité₀, D₀`) **+** params de flux (texture sol → `RU_base`, latitude, scénario climat `ΔT/×pluie`, seed maître) | une fois, au bootstrap |
| **`ScenarioContext`** (décisions agriculteur) | leviers : dose N, couverts %, résidus %, irrigation, gestion flore… | en continu, en transition douce 7-14 j |
| **`WeatherSeriesProvider`** (CSV) | série journalière réelle `t_min, t_max, precip` | lue 1 fois par tick |

**Rien d'autre n'entre.** Tout le reste est dérivé par le modèle.

## A.2 La boucle journalière (1 tick = 1 jour)

L'ordre des étapes **n'est pas cosmétique** : il garantit que chaque
variable lit des valeurs cohérentes. Les seules dépendances circulaires
du système (`C → N_min → N → Y → résidus → C`) sont résolues par un
**décalage d'un jour sur les variables lentes** (carbone, azote
minéralisé) — négligeable au pas journalier, et documenté ci-dessous.

| # | Étape | Lit | Écrit | Note de décalage |
|---|---|---|---|---|
| 1 | `Scenario.Tick` | cibles des décisions | leviers courants (interpolés) | transition 7-14 j |
| 2 | Météo du jour | CSV + `ΔT`/`×pluie` | `T_min,T_max,T_moy,pluie` ; fenêtres chaleur/canicule | **déterministe** |
| 3 | ETP de référence | `T_min,T_max,T_moy`, `Ra(lat, jour)` | `ETP_0` (Hargreaves) | `Ra` calculé, pas une entrée |
| 4 | Bilan hydrique sol | `ETP_0, pluie, RU_max(C_{j−1})` | `θ`, `Drainage` | `RU_max` lit `C` de la veille (C lent) |
| 5 | Nappe | `Drainage` | `h` | — |
| 6 | Demande & croissance | `θ` (stress `Ks`), `N_{j−1}` | `Y_cible`, demande azotée | — |
| 7 | Rendement | `Y_cible` | `Y` (relaxation EMA) | — |
| 8 | Azote | `Fert`, `N_min(C_{j−1})`, `N_fix`, prélèvement`(Y)`, `Drainage` | `N` | `N_min` lit `C` de la veille |
| 9 | Carbone (2 pools) | résidus`(Y)`, couverts, litière`(densité_{j−1})`, `r_e(T,θ)` | `C_y, C_o` | — |
| 10 | Densité haie (flore) | `θ, N`, gestion | `HedgerowDensity` | EMA lente (mois) |
| 11 | Biodiversité | `densité, θ, N`, canicule | `D` (EMA), pression instantanée | EMA ~1 an |
| 12 | Économie | `Y`, coûts, `ΔC`, subventions | `Marge`, `K` | — |
| 13 | `AdvanceDay` | — | `CurrentDay += 1` | — |

> **Pourquoi cet ordre ?** L'eau (`θ`) doit être calculée avant tout ce
> qu'elle stresse (rendement, flore, biodiv). Le rendement doit être connu
> avant le carbone (les résidus en dépendent) et avant l'azote (le
> prélèvement en dépend). La biodiversité est dernière des variables
> biophysiques (elle agrège habitat + eau + intrants à jour). L'économie
> clôt le tick (elle lit tout le reste).

## A.3 Déterminisme, run fantôme, transitions

- **Déterminisme** : seed maître → sous-flux par hash (splitmix64/FNV).
  La météo étant désormais **réelle et fixe**, le RNG ne sert plus qu'au
  **bruit des capteurs** et aux réalisations de projection. Même seed +
  même CSV + mêmes décisions → état identique (test B7).
- **Run fantôme** : un second moteur, **même CSV, mêmes seeds**, mais
  décisions **gelées** à leur valeur de départ (`CreateFrozenShadowFrom`).
  Comme la météo est identique au réel, **toute divergence réel↔fantôme
  est imputable aux seules décisions** — c'est ce qui alimente le KPI
  « apport de la techno » (B.3). Plus de bruit météo parasite (test B8).
- **Transitions** : aucune décision n'est abrupte. Un changement de levier
  interpole sa valeur sur 7-14 j (`TransitioningParameter`). Évite les
  sauts non physiques dans les flux.

## A.4 Trois niveaux à ne jamais confondre

```
   VÉRITÉ DU MODÈLE          CAPTEUR (bruité)          KPI / ÉVÉNEMENT
   (état réel simulé)   →   (mesure imparfaite)   →   (ce que l'agriculteur voit / déclenche)
   θ = 42 mm                sonde : 39 mm             alerte stress si mesure < seuil
   C = 47 tC/ha             tour Eddy : 45 (intégré)  KPI carbone affiché
```

- Les **KPI Hero** (Partie B) lisent en général la **vérité** (l'état que
  l'agriculteur « possède »).
- Les **événements** (déclencheurs de recos, Partie C) seuillent la
  **mesure capteur** — primauté du capteur : une alerte reflète ce qui est
  *mesuré*, pas ce que le modèle « sait ». C'est la distinction qui fait le
  *digital twin instrumenté*, pas un jeu.

---

# PARTIE B — Le calcul des KPI

## B.1 Principe général

Chaque KPI suit la même chaîne :

```
variable(s) d'état  →  formule de l'indicateur  →  valeur métier (unité réelle)
                                                 →  normalisation [0,1] pour la jauge couleur
```

La **normalisation** est une application linéaire vers `[0,1]` bornée par
des valeurs **ancrées sur le réel** (documentées par source). Elle ne sert
qu'à colorer la jauge (rouge→vert) ; la valeur affichée reste l'unité
métier (€/ha, t/ha, tC/ha…).

## B.2 Les 5 Hero KPI

### KPI 1 — Marge / rentabilité [€/ha/an] — *priorité agriculteur n°1*

```
Marge = Y · prix_culture
      − Charges_fixes
      − Coût_N(dose_N)
      − Coût_irrigation(volume)
      − Surcharge_climat
      + PAC_base + MAEC + PSE
      + Crédit_carbone(ΔC)
```

- `Coût_N = dose_N · prix_N`  (≈ 1,2 €/kgN ; *COMIFER, marchés intrants*).
- `Crédit_carbone = max(0, C − C_baseline) · (44/12) · prix_tCO2`
  (≈ 30-40 €/tCO₂ ; *Label Bas-Carbone*), annualisé. **Seul le gain de
  carbone au-dessus de la baseline est monétisé.**
- `PSE = densité_haie · taux_PSE` ; `PAC_base ≈ 220 €/ha` *(PAC 2025)*.
- **La haie n'apparaît plus en charge** (décision #1).
- **Normalisation** : linéaire `[−500, 1500] €/ha → [0,1]`, seuil de
  rentabilité ≈ 0,25 *(marges réelles Perche 100-400)*.

> *Exemple chiffré* : `Y=5,5 t/ha × 250 = 1375` ; charges 1200 ; N (dose
> 150) ≈ 180 → coûts ~1200 ; + PAC 220 + PSE 30 + crédit 10 ⇒
> **Marge ≈ 435 €/ha** → jauge ≈ 0,47 (vert clair). Sous sécheresse
> (`Y→3,8`), brut 950, marge ≈ **10 €/ha** → jauge ≈ 0,26 (rouge).

### KPI 2 — Rendement & sa stabilité [t/ha + σ]

- Valeur : `Y` (t/ha).
- **Stabilité** : `σ` inter-annuel du rendement (ou de la marge), estimé
  sur fenêtre glissante OU via le rejeu météo de la projection (Partie C).
  Affiché comme sous-indicateur « risque » (un sol résilient → `σ` bas).
- Normalisation : `[0, 1,2·Y_pot] → [0,1]` (Y_pot = 7,6 t/ha = potentiel *non
  stressé* après recalibration azote ; le rendement *actuel* affiché tourne
  ~5,5 t/ha avec ~13 % de CV inter-annuel, cf 08_MODELE §5.5).

### KPI 3 — Biodiversité [0,1]

- Valeur = `D` (état laggé, ce que la faune *vit* réellement) :
  ```
  D ← D + (1/τ)·(D_cible − D),   τ ≈ 1 an
  D_cible = w_h·hab(densité_norm) + w_w·eau(θ) + w_i·intrants(N) + climat(canicule)
  ```
- **Responsivité honnête** (B.4) : on expose AUSSI la `D_cible`
  instantanée (« pression ») pour que l'action de l'utilisateur *s'affiche*
  immédiatement, pendant que l'état `D` rejoint la cible avec son inertie
  réaliste. Le KPI Hero montre `D` ; une flèche fine montre la tendance
  (`D_cible` au-dessus/dessous).
- Normalisation : `D` est déjà `[0,1]`.

### KPI 4 — Carbone du sol [tC/ha]

- Valeur = `C = C_y + C_o`.
- Normalisation `[30, 100] → [0,1]` *(BDAT / INRAE 4‰)*.

### KPI 5 — Réserve en eau / stress hydrique [% RU]

- Valeur = `θ / RU_max · 100` (% de la réserve utile remplie). **Plus
  parlant pour l'agronomie que la profondeur de nappe.**
- Normalisation : `θ/RU_max` directement `[0,1]` (≤ ~20 % = stress sévère,
  *FAO-56*).
- La **profondeur de nappe** `h` reste affichée en second rideau (lecture
  capteur piézomètre), pas en Hero.

## B.3 Apport de la techno (réel vs fantôme)

```
Apport_techno(t) = ∫ [ Marge_réelle(τ) − Marge_fantôme(τ) ] dτ  −  Investissements
```

- Le run fantôme (mêmes CSV/seeds, décisions gelées) donne la
  contrefactuelle « si l'agriculteur n'avait rien instrumenté/décidé ».
- **NET d'investissement** : c'est la valeur réellement créée par
  l'instrumentation + les décisions. Borne d'affichage `[−500, 1500]`.

## B.4 Pourquoi exposer état laggé ET pression instantanée

Une biodiversité réaliste réagit en **mois/années**, pas en un tick. Mais
une UI où « je baisse les intrants et rien ne bouge à l'écran » est
frustrante et illisible. La solution honnête : l'**état** `D` (laggé) pilote
la faune visible et le KPI ; la **pression** `D_cible` (instantanée)
s'affiche comme tendance. L'utilisateur voit que son geste *compte*
immédiatement, tout en apprenant que l'écosystème *répond lentement*. Aucun
mensonge visuel.

## B.5 Panneaux Niveau B (rappel)

Sous les Hero, 3 panneaux détaillent (lecture seule) : **Biodiversité**
(4 facteurs habitat/eau/intrants/paysage + comptage faune visible), **Climat &
ressources** (T° moyenne, pluie cumulée, carbone, flux CO₂, `θ`, nappe),
**Économie** (rendement, coûts détaillés, PSE, PAC, crédit carbone, capital,
horizon de rentabilité). Tous dérivés des mêmes états/capteurs.

---

# PARTIE C — Le moteur de recommandation

C'est le cœur décisionnel. Objectif : des recommandations **rationnelles**
(fondées sur ce qu'un agriculteur optimise vraiment), **impactantes** (la
bonne dose, pas un micro-pas), et **expliquées** (provenance + effet projeté
+ coût + incertitude).

## C.1 Vue d'ensemble : le pipeline en 8 étapes

```
 ┌─ 1. CAPTEURS mesurent (bruité) ────────────────────────────┐
 │                                                            ▼
 │  2. DÉTECTION d'événement (seuil sur la MESURE)     [EventDetector]
 │                                                            │
 │  3. CANDIDATS : event → leviers faisables                  │
 │                                                            ▼
 │  4. DOSE OPTIMALE : pour chaque candidat, on cherche le    │
 │     niveau de levier qui maximise l'objectif  ◀── le fix P1 │
 │                                                            ▼
 │  5. PROJECTION forward (copie d'état, rejeu de N années)   [ModelOutcomeProjector]
 │     → ΔKPI worst/expected/best                              │
 │                                                            ▼
 │  6. OBJECTIF : U = NPV(Δmarge) − λ·σ(Δmarge) (+ écolo €)   [FarmerObjective]
 │     → on classe les candidats, on garde le meilleur        │
 │                                                            ▼
 │  7. SURFAÇAGE : WinWin / compromis / perdant ; auto-popup ? [RecommendationSurfacing]
 │                                                            ▼
 │  8. EXPLICATION + JOURNAL : payload complet, application    [DecisionJournal / AutoActionPipeline]
 └────────────────────────────────────────────────────────────┘
```

## C.2 Étape 1-2 — Détection d'événement (primauté du capteur)

Chaque jour, les capteurs lisent la vérité + bruit, puis l'`EventDetector`
seuille la **mesure** (jamais la vérité) :

| Événement | Capteur | Seuil (sur la mesure) | Source |
|---|---|---|---|
| **Stress hydrique** | **station météo — mesure d'humidité** (pas de capteur dédié au MVP) | `θ/RU_max < 20 %` (proxy humidité) ≥ 30 j | FAO-56 |
| Sécheresse nappe | piézomètre `h` | `h > 2,6 m`, 30 j | OFB/RMT (secondaire) |
| Anomalie faune | acoustique + caméra | indice mesuré `< 0,7` | Vigie-Nature |
| Carbone bas | tour Eddy (stock intégré) | `< 45 tC/ha` | INRAE 4‰ |
| **Carence azotée** `[NOUVEAU]` | bilan N (proxy mesuré) | `N` limitant le rendement (`Kn < 0,8`) | COMIFER |
| **Excès / lessivage N** `[NOUVEAU]` | bilan N + drainage | lessivage mesuré `>` seuil | COMIFER |
| Rentabilité basse | indicateur éco | marge `< 50 €/ha` | marges Perche |

> L'**humidité est repliée dans la station météo** (pas de capteur dédié au
> MVP) : c'est sa mesure d'humidité (pas le piézomètre) qui arme l'alerte
> agronomique. Les **deux événements azote** rendent le levier N
> bidirectionnel — c'est ce qui multiplie et fiabilise les recos (corrige
> « trop peu de recos »).

Garde-fou : cooldown 30 j par type ; dé-duplication via le journal
(un événement déjà couvert n'est pas re-traité chaque tick).

## C.3 Étape 3 — Candidats par événement

Un événement n'a pas UNE réponse mais un **ensemble de candidats faisables**
(filtrés par les bornes : on ne propose pas d'augmenter N si déjà au max) :

| Événement | Leviers candidats |
|---|---|
| Stress hydrique | Couverts (anti-évaporation) · Travail du sol réduit (rétention) — *réponses structurelles : en pluvial, pas de remède immédiat* |
| Anomalie faune | Baisser l'IFT · Baisser N · Réduire arrachage / planter haies · Couverts |
| Carbone bas | Couverts · Travail du sol réduit · Part de prairie |
| Carence azotée | Augmenter N · Couvert légumineux |
| Excès / lessivage N | Baisser N · Couvert « piège à nitrates » |
| Rentabilité basse | Augmenter N (si sous l'optimum & biodiv non critique) · viser un paiement MAEC/PSE (pratique éligible) |

## C.4 Étape 4 — La recherche de dose optimale (le fix de ton P1)

**Le problème corrigé** : aujourd'hui « baisser les intrants » applique un
pas fixe de 0,2 qui s'enchaîne sans jamais dire *le bon niveau*. **La
correction** : le moteur **cherche le niveau de levier qui maximise
l'objectif**, et recommande de l'atteindre en une décision informée.

Algorithme (1-D, par levier candidat) :

```
fonction DoseOptimale(levier, état, scénario, horizon):
    meilleurs_U = −∞ ; meilleure_dose = dose_actuelle
    pour niveau dans Discrétiser(plage_faisable(levier), K=7):
        U = ÉvaluerObjectif(levier @ niveau, état, scénario, horizon)   # via projection C.5-C.6
        si U > meilleurs_U: meilleurs_U, meilleure_dose = U, niveau
    # raffinement optionnel autour du meilleur (golden-section, 2-3 pas)
    retourner RaffinerAutour(meilleure_dose)
```

- `K = 7` niveaux + raffinement → ~10 évaluations par levier.
- La dose retenue devient la **valeur recommandée** ; le **slider de la
  popup s'ouvre dessus par défaut** (l'utilisateur peut s'en écarter).
- C'est une vraie optimisation, pas un pas magique : la dose remonte à un
  `argmax` traçable.

## C.5 Étape 5 — La projection forward

`ÉvaluerObjectif` simule réellement le futur (pas de coefficient figé) :

```
fonction Projeter(levier @ niveau, état, scénario, horizon):
    pour r dans 1..R(=9 réalisations météo):
        copie_base   = Snapshot(état, scénario)           # baseline : on ne fait rien
        copie_levier = Snapshot(état, scénario) ; Appliquer(levier @ niveau)
        météo_r = GénérerMétéo(climatologie, seed + r)     # tirage stochastique seedé
        simuler(copie_base, copie_levier, horizon, météo_r)
        Δmarge_r   = KPI_marge(copie_levier) − KPI_marge(copie_base)
        Δbiodiv_r  = KPI_biodiv(copie_levier) − KPI_biodiv(copie_base)
        # … idem carbone, eau
    retourner Distribution(Δ sur les R réalisations)        # worst / expected / best
```

- **Horizons** : court (30 j) + long (365 j / horizon).
- **`R` réalisations = R tirages stochastiques seedés** du générateur météo
  (climatologie de Tourouvre) → la bande d'incertitude (worst/expected/best)
  reflète la **variabilité inter-annuelle** simulée, pas un bruit inventé.
- **Coût maîtrisé** (WebGL) : la projection ne tourne **que quand un
  événement se déclenche** (pas chaque tick), via **coroutine** étalée sur
  quelques frames (règle « coroutines, pas async »). `K·R·horizon` borné ;
  cache des projections par `(levier, niveau)`.
- KPI injectés en **délégués** (`profitFn`, `biodivFn`, …) pour garder la
  Couche 03 indépendante de la Couche 04.

## C.6 Étape 6 — La fonction-objectif (rationnelle)

L'agriculteur ne maximise pas une somme pondérée arbitraire ; il **sécurise
une marge sous risque**, l'écologie entrant par des euros réels et par la
stabilité :

```
U(levier @ niveau) =  NPV(Δmarge sur horizon)            # valeur actualisée
                    − λ · σ(Δmarge)                       # aversion au risque
                                                          # (l'écolo est DÉJÀ dans Δmarge
                                                          #  via PSE/MAEC/crédit carbone)
NPV = Σ_t  Δmarge_t / (1+ρ)^t          ρ ≈ 4 % (taux d'actualisation)
σ   = écart-type de Δmarge sur les R réalisations météo
```

- **Monétisation** : biodiversité/carbone pèsent en euros (PSE, MAEC,
  crédit Label Bas-Carbone) → traçable, défendable, pas de poids inventé.
- **Résilience** : un sol/une réserve tamponnés réduisent `σ` → l'écologie
  « paie » aussi par la stabilité. C'est mesurable (les R réalisations).
- `λ`, `ρ` : calibrables, sourcés *(littérature agro-éco ; Edwards-Jones
  2006)*.

> **Comparaison avec l'existant** : on garde l'architecture de projection
> (elle est bonne), on remplace le `0,8·profit + 0,2·biodiv` par un objectif
> où l'arbitrage est *fondé* (€ + risque), et on ajoute la recherche de dose
> optimale. C'est la différence entre « une reco plausible » et « une reco
> qu'on peut justifier devant un jury ».

## C.7 Étape 7 — Surfaçage & classification

Le meilleur candidat par événement est classé selon le **signe** de son
effet long terme `(Δmarge, Δbiodiv)` :

| Classe | Condition | Auto-popup ? |
|---|---|---|
| **WinWin** | Δmarge ≥ 0 ET Δbiodiv ≥ 0 | toujours |
| **Compromis économique** | Δmarge ≥ 0, Δbiodiv < 0 | passif (liste) |
| **Compromis écologique** | Δbiodiv ≥ 0, Δmarge < 0 | seulement si biodiv critique (< 0,30) |
| **Perdant-perdant** | les deux < 0 | jamais (filtré) |

Garde : une reco n'est surfacée que si `ΔU > 0`. Une reco économique ne
fire pas si elle pousse la biodiversité sous le plancher critique.

## C.8 Étape 8 — Le payload d'explication (« expliquée »)

Chaque popup porte un objet complet, pour que la décision soit *comprise* :

```
Recommendation {
  provenance : "sonde humidité sol : θ = 18 % RU depuis 34 j → stress hydrique"
  levier     : "Irrigation ciblée"
  dose       : "+25 mm (niveau optimal calculé)"
  effet_projeté (avec bande) : {
      marge   : +90 €/ha  [worst +40 / best +130]
      rendement:+0,6 t/ha
      biodiv  : +0,01
      eau     : θ 18%→34% RU
  }
  coût        : 0 € (eau disponible) ; horizon_rentabilité : immédiat
  classe      : WinWin
  rationale   : "La réserve en eau est sous le seuil de stress ; l'irrigation
                 lève le stress et sécurise le rendement sans coût écologique."
}
```

La bande `[worst/best]` vient des R années météo (C.5) — l'incertitude est
*honnête et sourcée*.

## C.9 Application & journal

- L'utilisateur **accepte / reporte / refuse**. Tout passe par la popup,
  **mais** (règle reco ⊆ leviers) le même levier reste actionnable
  directement par son slider, hors recommandation.
- À l'acceptation, `AutoActionPipeline` amène le levier vers la dose
  optimale en **transition douce 7-14 j**.
- `DecisionJournal` enregistre verdict, dose appliquée, coût, jour ; marque
  l'événement couvert. Le run fantôme, lui, ignore tout ça (baseline gelée)
  → l'écart réel↔fantôme mesure l'effet de la décision (B.3).

## C.10 Exemple de bout en bout

> **Jour 612, été, scénario −40 % pluie.**
> 1. La station météo mesure une humidité à `θ = 17 % RU` (vérité 19 %,
>    bruit) depuis 33 j → **événement stress hydrique**.
> 2. Candidats faisables (irrigation coupée en MVP → réponses
>    **structurelles**) : Couverts d'interculture, Travail du sol réduit.
> 3. Dose optimale par candidat (recherche C.4) :
>    Couverts → 70 % ; Travail du sol → passer en TCS.
> 4-5. Projection (9 réalisations météo) — *l'année en cours est déjà
>    perdue ; le gain est sur l'exposition future* :
>    | Candidat @ dose opt. | NPV Δmarge (365 j) | Δσ marge (− = +résilient) | Δbiodiv |
>    |---|---|---|---|
>    | Couverts 70 % | +28 | −9 | +0,03 |
>    | TCS | +15 | −6 | +0,01 |
> 6. Objectif `U = NPV − λσ` (λ=0,5) : les **couverts gagnent** — gain marge
>    modeste **+ réduction du risque** (moins d'évaporation, sol couvert →
>    moins de stress au prochain coup de chaud).
> 7. `Δmarge ≥ 0` et `Δbiodiv ≥ 0` → **WinWin** → auto-popup.
> 8. Popup : « Semer un couvert (70 %) — *ne sauve pas cette sécheresse,
>    mais réduit ton exposition à la prochaine* : −évaporation, +0,03 biodiv,
>    +28 €/ha [10/45] sur l'horizon, éligible MAEC ». Le slider s'ouvre
>    sur 70 %.

## C.11 Comment ça corrige tes pathologies observées

| Symptôme (tes tests) | Cause racine | Correction |
|---|---|---|
| **P1** : « baisser N » par −0,2 en boucle, jamais le bon taux | pas de recherche d'optimum | C.4 recherche de dose optimale + slider par défaut dessus |
| **Trop peu de recos** | peu d'événements, leviers mono-directionnels | C.2 sonde humidité + 2 événements azote bidirectionnels |
| **Recos mal faites / optim imparfaite** | objectif arbitraire 0,8/0,2 | C.6 marge actualisée ajustée du risque + écologie monétisée |
| **« 10 ans −50 % et tout va bien »** | modèle-étoile (cf 08 §5) | le `θ`-bucket + couplages (doc 08) font enfin mordre la sécheresse |
| Décisions pas claires | payload pauvre | C.8 provenance + effet projeté + bande + coût + rationale |

---

# PARTIE D — Les leviers de décision (MVP, curés par impact)

Contrainte MVP : **peu de leviers, mais chacun à impact fort** (déplace
visiblement ≥1 KPI et génère des recos nettes) **et à downside réel** —
pour un **optimum intérieur** (« optimiser, pas moraliser »). Sans
downside, l'écologie serait soit gratuite (faux), soit un sacrifice pur
(moralisateur).

## D.1 Le jeu retenu (MVP)

| Levier | Effet + | Downside − (simulé) | Recos | Nouveaux états |
|---|---|---|---|---|
| **Fertilisation azotée** | rendement/marge ↑ | biodiv ↓, lessivage ↑ | carence→+N ; excès→−N | azote (§5.4 de 08) |
| **IFT / pesticides** | rendement protégé, coût maîtrisé | biodiv ↓↓ (insectes, Hallmann) | réduire l'IFT | — |
| **Travail du sol** (labour↔TCS↔SD) | carbone ↑, eau θ ↑, carburant ↓ | adventices `W` ↑ (→ IFT ou rendement), immobilisation N, sols lourds | passer en TCS/SD | `W` pression adventices |
| **Couverts d'interculture** | carbone ↑, N (fix/piège), évaporation ↓ | coût semence, gestion | semer un couvert | — |
| **Gestion flore / haies** | biodiv ↑ (habitat), visuel | lent ; coût plantation | planter / réduire l'arrachage | — |
| **Part de prairie** (culture↔prairie) | carbone ↑↑, biodiv ↑↑, σ marge ↓ | **marge ↓↓** (renonce au revenu culture) + **lock-in PAC** | convertir une parcelle | compartiment prairie (revenu fourrager léger) |

## D.2 Coupé / différé (discipline MVP)

- **Restitution des résidus** : impact faible et lent → **fusionnée** dans
  une hypothèse de modèle (retour résidus par défaut), plus un levier.
- **Fertilisation organique (fumier)** : **différée post-MVP** — impact
  moyen, dépend d'une source d'effluents (élevage). Revient *avec* l'élevage.
- **Irrigation** : **coupée en MVP (validé)** — peu réaliste en Perche
  (pluvial). La sécheresse se gère par les leviers *structurels* (couverts,
  travail du sol, SOC→réserve en eau) ; irrigation = adaptation post-MVP.
- **Variété/date de semis, bandes enherbées, têtards** : **coupés MVP**
  (niche / impact faible).

## D.3 La prairie : revenu léger en MVP

Pour rester *optimisable* sans modéliser un troupeau, la prairie produit un
**fourrage valorisé à €X/t** (vente/valorisation), nettement < marge
céréale. Le **downside reste fort** (perte de revenu culture + lock-in
réglementaire). Post-MVP : un **élevage simple** (chargement → fourrage →
lait/viande + **fumier** + méthane) débloque à la fois un meilleur revenu
prairie ET la fertilisation organique — **bundle cohérent** pour plus tard.

## D.4 La couche de paiement (MAEC / PSE / Label Bas-Carbone)

Ce **ne sont pas des leviers** : c'est une couche qui **paie les pratiques
explicites** quand leurs critères sont remplis (calculée chaque année sur
l'état) :
- MAEC réduction phyto : `IFT ≤ seuil` → €/ha.
- MAEC couverts/prairie : `couverts ≥ X` / prairie maintenue → €/ha.
- PSE haies : densité maintenue/accrue → €/m.
- Label Bas-Carbone : `ΔSOC > baseline` → crédit €/tCO₂.

La décision reste la **pratique** ; l'argent suit. Le **contrat impose une
durée** (maintenir N ans sinon remboursement) = downside de **lock-in**
simulé. La monétisation entre ainsi comme terme € traçable dans l'objectif
(C.6), sans poids arbitraire.

---

# Ce qu'il reste à figer après ta lecture

1. Valeurs précises : `ρ` (actualisation), `λ` (aversion risque), `K`
   (granularité dose), `R` (réalisations), seuils capteurs.
2. Détail complet du **module azote** (équations §5.4 de 08 à pousser).
3. La **passe de vérification mathématique** (équilibres, unités, signes)
   — le « gate papier » avant implémentation headless.

Tu lis, tu réagis, et on descend sur le point qui te semble le plus à
risque.
