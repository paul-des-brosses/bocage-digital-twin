# BACKLOG.md — Items post-MVP

Évolutions envisagées au-delà du MVP. L'ordre n'est pas une priorité ;
chaque item porte son estimation, ses dépendances et son origine.

> **Mis à jour 2026-06-11.** La **refonte intégrale du backend** (I1-I6) et le
> **cutover S5** sont livrés : modèle eau/carbone/azote/rendement recalibré, UI
> rebranchée, ancien code supprimé. Les items ci-dessous restent post-MVP ; le
> **Reporter de session** (cf CLAUDE.md §5.4) reste à construire.

---

## 1. Polish visuel & expérience

### #1 — Particules Unity (feuilles, poussières)

**Pourquoi reporté** : polish visuel pour post-MVP.

**Cible** :

- Feuilles dérivantes (densité pilotée par `RC_HedgerowHealth`).
- Poussière dans la lumière au-dessus des chemins (densité pilotée
  par `1 - RC_SoilMoisture`).

**Garde-fou §9** : la chute des feuilles doit dériver d'une mesure (le
déclin de `RC_HedgerowHealth`), jamais du mois calendaire — « automne »
est un thème visuel, pas le déclencheur. Même exigence que #6.

**Contrainte** : pas de threading (WebGL), pas de simulation physique
lourde. Particle System Unity standard, pre-warm activé.

**Estimation** : 0.5 jour.

---

### #2 — Effets visuels avancés mare et prairie

**Pourquoi reporté** : la v1 fait juste un lerp de couleur. Les
`.shader` actuels sont étendables sans bouleverser l'architecture.

**Idées concrètes** :

- `S_Pond` : rides sinusoïdales basse fréquence sur l'alpha, modulées
  par `_WaterLevel`. Reflet de ciel.
- `S_Meadow` : variation florale (clusters de petites taches
  colorées) modulée par `RC_SoilMoisture`.

**Estimation** : 0.5-1 jour par shader.

---

### #3 — Animations UI (transitions, micro-interactions)

**Pourquoi reporté** : v1 fonctionnelle, pas léchée.

**Cibles** :

- Fade-in 200 ms des panneaux à l'ouverture.
- Tween des valeurs numériques (`Mathf.Lerp` côté binding).
- Pulse léger sur les cartouches Hero KPI quand la valeur a bougé
  significativement.

**Estimation** : 0.5 jour.

---

### #4 — SessionReporter accessible depuis l'UI

**Pourquoi reporté** : pas critique pour la première publication
démontrable.

**Cible** : bouton « Exporter la session » dans le dashboard qui
sérialise (JSON dans console / fichier WebGL téléchargeable) le
déroulé : seed, scénario, journal de décisions (avec actions
manuelles via journal ADR #47), courbes Hero KPIs.

**Estimation** : 0.5 jour.

---

### #5 — Popup explicative du KPI « Apport de la techno » avec mini-chart real vs shadow

**Pourquoi reporté** : le KPI « Apport de la techno » est un chiffre
nu. Un visiteur qui veut visualiser la divergence n'a pas d'accès
direct à l'historique courbe.

**Cible** : picto `(i)` à côté du libellé « Apport de la techno ». Au
clic, popup centrée : texte court explicatif + mini-chart 400×140 px
(60 derniers jours, real solide vs shadow pointillé) + bouton Fermer.

**Pré-requis partiel** : la mutualisation `ISensorHistory<T>` livrée
en E6 (ADR #53) couvre déjà le besoin de ring-buffer pour les
mesures. Pour les KPIs real/shadow, ring-buffer similaire à
construire dans `SimulationRunner`.

**Estimation** : 4 h.

---

### #6 — Effets visuels saisonniers (ciel, prairie)

**Pourquoi backlog** : modulation visuelle du ciel et de la prairie
selon la T° saisonnière et les conditions météo journalières.

**Cible** :

- Shader `SG_Sky` : moduler la couleur selon la T° saisonnière (ciel
  d'hiver pâle/bleu, ciel d'été chaud).
- Shader `S_Meadow` : moduler la teinte selon la T° + humidité (vert
  frais printemps, jauni été sec).

**Garde-fou critique (§9)** : ces effets DOIVENT être dérivés du
modèle (T°, humidité) et non du mois en tant que tel. Le mois n'est
pas une variable mesurée — la T° et l'humidité le sont.

**Pré-requis** : E2 livré.

**Estimation** : 0.5-1 jour par shader.

---

## 2. Calibration & cohérence scientifique

### #7 — Réalisme avancé des capteurs faune

**Pourquoi reporté** : pour le MVP, le piège photo et le capteur
acoustique partagent un profil de bruit identique et statique
(σ = 0.20 / √fauna). Suffisant pour le MVP mais insuffisant pour
simuler les vraies limites de chaque technologie.

**Cibles** :

- Signal acoustique dégradé par la météo (vent, pluie). Pré-requis :
  saisonnalité météo E2 — disponible.
- Piège photo moins efficace en bocage très dense.
- Biais saisonniers (oiseaux vocaux au printemps, amphibiens
  détectables en période humide).
- Score de confiance affiché dans l'UI (intervalle 95 %).

**Pré-requis** : E2 livré (saisonnalité), E6 livré (panneau
inspection capteurs où afficher l'intervalle de confiance).

**Estimation** : 1 jour.

---

### #8 — Recalibrer `MaintenanceCost` selon le référentiel AFAC 2024

**Pourquoi reporté** : la valeur actuelle de 1 €/m/an est défendable
comme coût out-of-pocket mais 3-5× inférieure aux références
sectorielles (Réseau Haies 2024 : 3,69 €/ml gestion durable,
amendement Sénat nov. 2025 : 4,5 €/ml).

**Cible** : rendre le taux paramétrable dans `ScenarioContext` (slider
« mode d'entretien » de auto-réalisation 1 €/ml à prestation 5 €/ml).
Lier au capital (ADR #50) puisque les deux touchent au modèle
économique.

**Lien** : cas concret du sourçage d'ensemble visé par #11.

**Estimation** : 0.5 jour.

---

### #9 — Reformuler la croissance des haies comme proxy explicite

**Pourquoi reporté** : `AnnualGrowthMetersPerHectare = 0.5` est
sémantiquement ambigu (densification fonctionnelle, pas allongement
linéaire). Fourchette AFAC régénération 0.2-0.4 m/ha/an suggère que
0.5 est dans le haut de fourchette.

**Cible** :

- Renommer en `AnnualDensificationProxyMetersPerHectare` ou XML doc
  clair.
- Recalibrer sur 0.2-0.4 si arbitré par agronome.
- Documenter dans `DECISIONS.md` la distinction.

**Lien** : sous-cas concret de l'audit #11 (qui le liste déjà).

**Estimation** : 0.3 jour + arbitrage agronome.

---

### #10 — `OutcomeProjector` state-aware (dérivé du modèle) — ✅ traité (2026-06-05)

**Statut** : **résolu** par le chantier modèle vivant (ADR #62). L'ancien
`OutcomeProjector` à coefficients figés est remplacé par
`ModelOutcomeProjector`, qui simule chaque levier en avant sur une copie de
l'état (vrai moteur, 3 réalisations météo) et prend le ΔKPI réel. L'escalade
de surfaçage écologique est active, et l'optimum de profit n'est plus chiffré
en dur (il émerge de la projection). Les deux bénéfices ciblés ci-dessous sont
livrés.

**Origine** : chantier E8-E9 (2026-06-04). Incohérence Priority-1 de
l'audit interne du modèle.

**Pourquoi backlog** : les projections d'outcome du popup décision
(`OutcomeProjector`) sont des **coefficients figés** par type de reco,
pas des dérivations du modèle dans l'état courant. Bon ordre de
grandeur et bon signe, mais elles divergent de l'effet réel (la baisse
d'intrants promet +0,10 de biodiv long terme là où `FaunaDynamicsRule`
en donne ~+0,014 à un pas de −20 %). Suffisant pour le MVP, documenté
comme limite dans CALIBRATION.md §E8-E9.

**Cible** :

- Calculer (Δprofit, Δbiodiv) d'une action depuis l'état courant via
  les règles recalibrées, au lieu des coefficients figés.
- Bénéfice 1 : corrige l'incohérence projecteur↔modèle.
- Bénéfice 2 : **active l'escalade de surfaçage** — un compromis
  *écologique* (profit↓ / biodiv↑) remonterait en popup si
  biodiv < 0.30. Aujourd'hui dormante, faute de reco écolo classée
  « compromis » par le projecteur figé.

**Pré-requis** : petit refactor de couche — les constantes économiques
(`CropPrice`, `BasicCapPayment`, `PacHedgeBonus`) sont en Couche 04
(`IntegratedProfitabilityIndicator`) alors que le projecteur est en
Couche 03. Les remonter en Couche 01 (`FarmEconomics`) débloque le
calcul.

**Estimation** : 1-1.5 jour.

---

### #11 — Sourçage des constantes encore arbitraires (audit interne)

**Origine** : audit interne du modèle (chantier E9, 2026-06-04). ~31 %
des calculs étaient « arbitraires » (posés sans justification sourcée).

**Pourquoi backlog** : non bloquant pour le MVP (les calculs cœur —
rendement, coûts, profit, faune, carbone — sont sourcés), mais à
durcir pour la rigueur scientifique avant un usage sérieux.

**Cible (par priorité d'audit)** :

- **Nappe** (`WaterTableDynamicsRule`) : ✅ traité (2026-06-05) — refonte en
  bilan à réservoirs type GARDÉNIA, sourcée BRGM/SIGES Seine-Normandie +
  Eau Seine-et-Marne. Fin du « non validé hydrologiquement ».
- **Poids du composite biodiv** (40 % habitat / 25 % eau / 35 %
  intrants) : justification qualitative (Krefeld / MNHN) mais pas
  d'analyse de sensibilité ni de source chiffrée des pondérations
  exactes.
- **Croissance des haies** : forme désormais sourcée (f(eau, fertilité),
  INRAE/AFAC) ; restent à resserrer les seuils du facteur fertilité, le
  seuil eau faune 8 %/m et la pénalité canicule 0,01/jour.

**Estimation** : 1-2 jours (recherche + tests de sensibilité).

---

## 3. Extensions du modèle (leviers, événements, recommandations)

### #12 — Leviers décisionnels supplémentaires (pistes)

**Pourquoi backlog** : pistes de recommandations / leviers non encore
construites, dans le prolongement du moteur de recommandations.

- Recos : **fauche tardive / bande enherbée** (Gargamel, pucerons
  −30-50 %), agroforesterie inter-rangs, drainage léger, reconnexion
  mare/fossé (cf #16).
- Événements : excès de pluviométrie, pression ravageurs (caméra —
  cf #17).
- Leviers continus : calendrier de fauche, ratio prairies permanentes /
  temporaires (cf #20).

**Garde-fou** : toute nouvelle reco déclenchée par une mesure (§9),
chaque levier calibré et sourcé.

**Estimation** : ~1 jour par type de reco supplémentaire.

---

### #13 — Recommandations préventives (anticipatives)

**Pourquoi reporté** : les recommandations du MVP sont toutes
réactives — un seuil est franchi, on alerte. Un DT de support à la
décision propose aussi des recommandations anticipatives, basées sur
des tendances détectées avant le franchissement de seuil.

**Architecture cible** :

- `TrendDetector` en Couche 2, ring-buffer de 60 jours par variable
  surveillée + pente glissante.
- `TrendDetectedEvent` (variable observée, horizon prédit, confiance).
- Mappings préventifs candidats : tendance nappe, anomalie acoustique
  en formation, saisonnalité défavorable prévue (utilise E2 livré).

**Pré-requis** : ring-buffer par variable (peut réutiliser
`ISensorHistory<T>` livré en E6).

**Estimation** : 1-1.5 jour.

---

### #14 — Levier diversification des cultures

**Pourquoi reporté** : le mix de cultures est figé à 70 % blé tendre /
30 % colza dans `CropYieldDynamicsRule`. Pas de levier diversification,
alors que c'est l'une des voies d'accès à l'écorégime PAC et un outil
majeur de résilience.

**Cible** :

- Nouveau paramètre `CropDiversityIndex` dans `ScenarioContext`.
- Refonte `CropYieldDynamicsRule` : rotation diversifiée → rendement
  moyen légèrement plus bas (−5 % à −10 %) mais variance réduite et
  meilleure résilience.
- Effet économique : ouverture écorégime supérieur (46 → 63 €/ha
  PAC 2025).
- UI : slider continu « Diversification de l'assolement ».

**Lien** : le slider `CropDiversityIndex` est le **même** que celui de
#20 (qui en exploite l'effet biodiversité / diversité paysage) — à
concevoir comme un seul levier à deux effets.

**Estimation** : 1 jour.

---

### #15 — Événement échec de plantation

**Pourquoi reporté** : 30-50 % des plants meurent les 3 premières
années (sécheresse, broutage, défaut entretien). Reco PlantHedges sans
aléa sous-estime le coût et le risque réel. Rendrait l'arbitrage plus
honnête.

**Architecture cible** :

- `PlantingCohort` (date, magnitude, état vivant/échoué, conditions
  hydriques).
- `PlantingMortalityRule` calcule la fraction de mortalité.
- `PlantingFailureEvent` si > 30 % mortalité.
- `CompletePlantingRecommendation` : compléter la plantation.

**Pré-requis** :

- E5 livré (capital + horizon — sans coût représenté, l'échec n'a pas
  de poids économique).
- #8 (MaintenanceCost recalibré) — pour que le coût de l'entretien
  renforcé soit visible.

**Estimation** : 1-1.5 jour.

---

### #16 — Gestion de la mare (double usage piézomètre + événement + reco)

**Pourquoi reporté** : la mare est présente visuellement et citée dans
les sources (amphibiens, OFB / RMT Zones humides) mais aucun événement
ni reco ne lui est dédié.

**Cible** :

- Extension logique du piézomètre : seconde variable observée
  `PondWaterLevelMeters`.
- `PondDynamicsRule` (forte sensibilité évapotranspiration estivale —
  cohérent avec E2 saisonnalité).
- `PondDryingOutEvent` si < 0.2 m sur 14 jours consécutifs.
- `PondMaintenanceRecommendation`.
- Effet biodiversité (composante amphibiens isolée — couplable avec
  `RC_FaunaFactor*` livré en E5).
- Effet visuel : sprite mare modulé par `PondWaterLevelMeters`
  (extension du `S_Pond` actuel, cf #2).

**Pré-requis** : E2 livré (saisonnalité débloque la dynamique
évaporation), idéalement E5 livré (pour isoler proprement la
composante amphibiens).

**Estimation** : 1 jour.

---

### #17 — Cadre santé végétale complet

**Pourquoi backlog** : le chalara a été purgé (ADR #46) ; réintroduire
une seule maladie isolée n'est pas envisagé. Soit on remet un
écosystème santé végétale complet, soit rien.

**Cible** : modélisation cohérente d'une catégorie pathologies +
ravageurs sur les 3 cultures et essences du modèle :

- **Frêne** : chalara fraxinea (capteur drone NDVI ou enquête terrain
  phénologique).
- **Blé tendre** : rouille brune, septoriose (drone NDVI + observation).
- **Colza** : sclérotinia (observation phénologique).
- **Chêne / haies** : processionnaire chêne (piège à phéromones).

Avec :

- Capteurs adaptés à chaque pathogène (le piège photo IR ne convient
  pas — sémantique correcte).
- Événements détectables.
- Recommandations algorithmiques associées (rotation, traitements,
  élagage sanitaire).

**Pré-requis** : #18 (phénologie cultures) — sans phénologie, les
maladies cultures n'ont pas de fenêtre temporelle réaliste.

**Garde-fou** : à ne pas réintroduire item par item — soit on remet
tout l'écosystème santé végétale d'un coup, soit rien (conforme
CLAUDE.md §17).

**Estimation** : 2-3 jours.

---

### #18 — Phénologie cultures (semis, dormance, récolte)

**Pourquoi backlog** : la saisonnalité météo est livrée en E2 (ADR
#52), mais la phénologie cultures (semis, dormance, récolte, GDD,
fenêtre stress hydrique reproductive) reste un chantier post-MVP.

**Cible** :

- `GrowingDegreeDays` (somme T° base 6 °C depuis semis) en variable
  d'état dérivée de `CurrentWeather.TemperatureCelsius` livré en E2.
- Fenêtre semis (jour 280 ≈ octobre pour blé d'hiver) et fenêtre
  récolte (cumul GDD seuil).
- `CropYieldDynamicsRule` reconnaît les phases : croissance active vs
  dormance vs récolte (drop à 0 puis re-build).
- Nouvel événement « stress hydrique en phase reproductive » + reco
  associée.

**Calibration** : INRAE échelle BBCH blé, ARVALIS Eure-et-Loir.

**Pré-requis** : E2 livré (saisonnalité météo).

**Estimation** : 1.5-2 jours.

---

### #19 — Crises saisonnières manuelles (canicule, inondation) — sandbox

**Pourquoi backlog** : crises déclenchables manuellement par
l'utilisateur dans la section simulation, avec effets cascade visuels
et mécaniques sur le modèle. Plutôt outil de démo / sandbox que cœur
de thèse — **basse priorité**.

**Cible** :

- Bouton « Déclencher une crise » dans la section simulation (UI
  Toolkit).
- 2 types de crises : canicule (pic T° prolongé 7-14 jours),
  inondation (pic précip + remontée nappe brutale).
- Effets visuels associés (couleur ciel, prairie). À coupler avec #6.
- Effets mécaniques sur les variables d'état du modèle (cohérents avec
  les règles biophysiques existantes).

**Pré-requis** : E2 livré (saisonnalité), idéalement #6 livré (effets
visuels saisonniers de base).

**Estimation** : 1 jour.

---

### #20 — 4ème facteur biodiv « Diversité paysage »

**Pourquoi backlog** : les 3 facteurs exposés (habitat, eau, intrants)
sont livrés en E5 (ADR #51). Le 4ème facteur Diversité paysage reste
post-MVP.

**Cible** :

- Nouveau facteur `LandscapeDiversityFactor` calculé Shannon-like
  depuis les % prairies permanentes et la diversité des cultures.
- Nouveaux sliders scenario : `GrasslandPercent` (0-100 %) et
  `CropDiversityIndex` (1-5).
- Recalibration des pondérations à 4 facteurs.
- Affichage 4ème sous-indicateur dans l'onglet Biodiv (extension du
  binding livré en E6).

**Lien** : le slider `CropDiversityIndex` est le **même** que celui de
#14 (effet économique) — à concevoir comme un seul levier à deux effets.

**Bénéfice** : courbes de réponse plus fines par espèce visible
(`FaunaPool` livré en E4).

**Pré-requis** : E5 livré (3 facteurs déjà exposés), E6 livré (onglet
Biodiv finalisé).

**Estimation** : 4-6 h.

---

### #21 — Gestion biodiversité espèce-résolue : régulation (chasse) + réintroduction

**Origine** : demande utilisateur du 2026-06-03, étendue le 2026-06-07
pour absorber le couplage **faune → végétation** (capacité de charge /
herbivorie) écarté du chantier E11 (A2) : dans le modèle actuel l'indice
faune composite est borné et ne surpopule jamais, donc un seuil de
capacité de charge ne se déclencherait pas — il faut d'abord le modèle
espèce/guilde résolu ci-dessous. Vérifiée cohérente avec la thèse et le
modèle, sous conditions (voir garde-fous), de taille V2.

**Pourquoi backlog** : la faune est aujourd'hui un **indice composite**
unique (`FaunaPopulation` / `FaunaDynamicsRule`) ; les 4 espèces
visibles (`FaunaPool`, E4) ne sont qu'un reflet décoratif de cet
indice, pas des populations simulées. Gérer « certaines espèces »
suppose donc d'abord de passer à un modèle **espèce / guilde résolu**
(dynamiques de population par espèce) — extension significative.

**Cible** (deux leviers, fit thèse asymétrique) :

- **Régulation / chasse** (sanglier, chevreuil en surpopulation :
  dégâts cultures, haies, régénération). *Fort* fit thèse : levier
  éco↔rentabilité réel (surpopulation = dégâts rendement ; régulation =
  rendement protégé + régénération saine) et **instrumentable** — le
  piège photo (`CameraTrapSensorReader`, déjà livré) détecte la densité
  → événement surpopulation → reco de régulation.
- **Réintroduction / import d'espèces** (une fois l'habitat capable de
  les soutenir). Fit thèse *plus faible* : action de conservation, peu
  liée à la rentabilité et peu pilotée par un capteur — **à confirmer
  ou à écarter**.
- **Faune → végétation** (couplage écologique, ex-A2 #4) : une fois les
  populations résolues, une **pression d'herbivorie / capacité de charge**
  freine la croissance végétale au-delà d'un seuil K (dégâts sanglier sur
  la régénération 40-90 % en surdensité — CNPF/ONF ; herbivorie d'insectes
  ~−13 % — Visakorpi et al. 2024) — la **boucle auto-stabilisante
  faune ↔ végétation** que l'annexe A2 du registre décrivait. Inactif tant
  que la faune est un indice borné ; nécessite le modèle espèce/guilde.

**Garde-fous (conditions de cohérence — non négociables)** :

- **Distinguer pression-ravageur et valeur-biodiversité** : abattre un
  sanglier surnuméraire ne doit PAS lire « biodiversité en baisse »
  dans l'indice. Le modèle doit gagner un axe « surpopulation /
  pression » distinct de l'indice biodiv, sinon la chasse contredit le
  cadre « plus de faune = plus vert ».
- **Réintroduction conditionnée à l'habitat** : effet gaté par l'état
  mesuré (densité de haies, nappe, intrants au-dessus de seuils). On ne
  doit pas pouvoir « importer » son chemin vers une biodiversité élevée
  — l'habitat doit réellement soutenir l'espèce, sinon c'est un trucage
  de l'indice (viole §9 / §17).
- **Primauté du capteur (§9)** : chaque effet reste dérivé d'une mesure
  (densité caméra pour la régulation ; indicateurs d'habitat pour la
  maturité de réintroduction). Aucune logique scénique.

**Pré-requis** : modèle faune espèce / guilde résolu (extension de
`FaunaDynamicsRule`). Synergies avec #7 (réalisme capteurs faune) et
#20 (diversité paysage).

**Estimation** : large (4-7 jours) — l'essentiel est l'extension
multi-espèces du modèle, pas les boutons.

---

*Traçabilité : chaque item référence le chantier (`ROADMAP.md`) ou
l'ADR (`DECISIONS.md`) d'origine.*
