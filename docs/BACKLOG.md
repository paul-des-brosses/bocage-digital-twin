# BACKLOG.md — Items reportés hors scope MVP

Document réorganisé le 2026-05-28 après session de recadrage externe.

L'ancien backlog (items #1 à #23) a été retraité au regard du nouveau
scope MVP (cf `CLAUDE.md` §17 et ADR #45) :

- Certains items ont été **basculés dans le scope MVP** et sont
  traités par les chantiers E1-E7 de `ROADMAP.md`.
- Certains items ont été **supprimés définitivement** (chalara
  purgé, ou refus explicite hors MVP).
- Les autres restent en backlog post-MVP avec leur numérotation
  historique conservée pour traçabilité `git log`.
- Les nouveaux items issus de la session de recadrage sont numérotés
  à partir de **#24**.

L'ordre dans le document n'est pas un ordre de priorité — chaque
item porte sa propre estimation et ses dépendances.

---

## 1. Items basculés dans le scope MVP (sortis du backlog)

| Ancien ID | Description courte | Chantier MVP | ADR |
|---|---|---|---|
| #1 | Faune statique en pool (4 espèces) | E4 | #49 |
| #2 | Animation faune idle (frame-swap) | E4 | #49 |
| #9 | Capital d'investissement | E5 | #50 |
| #12 (partie saisonnalité météo) | Saisonnalité dans `WeatherUpdateRule` (Markov + normales mensuelles) | E2 | #52 |
| #13 | Variable d'état Carbone sol | E3 | #48 |
| #15 (partie 3 facteurs exposés) | Refonte biodiv — exposition habitat / eau / intrants | E5 | #51 |

La partie **phénologie cultures** de l'ancien #12 (semis, dormance,
récolte, GDD, fenêtre stress hydrique en phase reproductive) reste
en backlog. Reformulée en item **#25**.

La partie **4ème facteur Diversité paysage** de l'ancien #15 reste
en backlog. Reformulée en item **#28**.

---

## 2. Items supprimés définitivement (chalara + healthT faune)

| Ancien ID | Description | Raison |
|---|---|---|
| #3 | Modulation `_HealthT` sur la faune | Hors MVP (cf ADR #49). Pas de réintroduction prévue. |
| #14 | Couplage sécheresse → chalara | Chalara purgé (cf ADR #46). |
| #16 | Détection chalara avec capteur adapté | Chalara purgé. Réintroduction conditionnelle à un cadre santé végétale complet (cf item #24). |

---

## 3. Items historiques livrés (conservés pour traçabilité)

### #5 — SG_Hedgerow node `_HealthT` natif

**Statut** : ✅ livré en sub-étape 10b. Le Shader Graph
`SG_hedgerow.shadergraph` lit la propriété `_HealthT` via un Lerp
inséré entre le mix densité et le multiply texture.

### #12 (saisonnalité météo) — Markov + normales mensuelles dans `WeatherUpdateRule`

**Statut** : ✅ livré en chantier E2 (cf ADR #52) le 2026-05-29.
`WeatherUpdateRule` lit désormais les normales mensuelles
Mortagne-au-Perche encodées dans `SeasonalWeatherDataDefaults`
(Couche 01, exposable via le SO `SeasonalWeatherDataAsset` en
Couche 05) et tire chaque jour : `Bernoulli(p_wet[mois])` puis
`LogNormal(mu[mois], sigma[mois])` pour les précipitations
(`MarkovRainModel`, sous-flux `"markov-rain"`), et `N(T_mois, σ=2)`
pour la T° (sous-flux `"weather-noise"`). Les anomalies scenario
restent additives sur T° et multiplicatives sur précip. Un widget
« Mois de démarrage » détermine la phase d'entrée du cycle, snapshoté
par la rule à la construction du moteur (changement effectif au
prochain `Rebuild`). Le `WeatherStationReader` (Couche 02) lit les
mesures avec bruit gaussien (σ_T = 0.3 °C, σ_précip = 5 %
relatif) et conserve une fenêtre glissante 365 j pour le futur
panneau d'inspection (E6 / ADR #53). Extension `CropYieldDynamicsRule` +
`InputCostDynamicsRule` : compteur 30 j de jours > 25 °C alimente
un terme additionnel de pénalité (rendement) et de surcharge
(intrants), additif sur les anomalies scenario.

La **phénologie cultures** (semis, dormance, récolte, GDD) reste
en backlog post-MVP (item #25).

---

## 4. Items reportés post-MVP (numérotation historique conservée)

### #4 — Particules Unity (feuilles, poussières)

**Pourquoi reporté** : choix utilisateur — polish visuel pour
post-MVP.

**Cible** :

- Feuilles dérivantes en automne (densité pilotée par
  `RC_HedgerowHealth`).
- Poussière dans la lumière au-dessus des chemins (densité pilotée
  par `1 - RC_SoilMoisture`).

**Contrainte** : pas de threading (WebGL), pas de simulation physique
lourde. Particle System Unity standard, pre-warm activé.

**Estimation** : 0.5 jour.

---

### #6 — Effets visuels avancés mare et prairie

**Pourquoi reporté** : la v1 fait juste un lerp de couleur. Les
`.shader` actuels sont étendables sans bouleverser l'architecture.

**Idées concrètes** :

- `S_Pond` : rides sinusoïdales basse fréquence sur l'alpha, modulées
  par `_WaterLevel`. Reflet de ciel.
- `S_Meadow` : variation florale (clusters de petites taches
  colorées) modulée par `RC_SoilMoisture`.

**Estimation** : 0.5-1 jour par shader.

---

### #7 — Animations UI (transitions, micro-interactions)

**Pourquoi reporté** : v1 fonctionnelle, pas léchée.

**Cibles** :

- Fade-in 200 ms des panneaux à l'ouverture.
- Tween des valeurs numériques (`Mathf.Lerp` côté binding).
- Pulse léger sur les cartouches Hero KPI quand la valeur a bougé
  significativement.

**Estimation** : 0.5 jour.

---

### #8 — Enrichir les leviers décisionnels de l'agriculteur

**Pourquoi reporté** : le MVP livre 3 types de recommandations
(replanter haies, irrigation, baisser intrants) déclenchées par
2 événements (drought, anomalie acoustique faune) après purge
chalara. Plus 3 actions manuelles équivalentes via journal (ADR #47).

**Pistes d'extension** :

- Nouveaux types d'événements : excès de pluviométrie, pression
  ravageurs (lecture caméra), épuisement fertilité sol.
- Nouvelles recommandations : couverts végétaux d'interculture
  (couplable avec carbone sol ADR #48), agroforesterie inter-rangs,
  drainage léger, fauche tardive, reconnexion mare/fossé.
- Nouveaux leviers continus dans le panneau de droite : calendrier
  de fauche, ratio prairies permanentes/temporaires.

**Garde-fous** :

- Toute nouvelle reco doit être déclenchée par un événement traçable
  à un capteur (CLAUDE.md §9).
- Chaque levier doit avoir une calibration sourcée.

**Estimation** : 1 jour par type de reco supplémentaire.

---

### #10 — SessionReporter accessible depuis l'UI

**Pourquoi reporté** : pas critique pour la première publication
démontrable.

**Cible** : bouton « Exporter la session » dans le dashboard qui
sérialise (JSON dans console / fichier WebGL téléchargeable) le
déroulé : seed, scénario, journal de décisions (avec actions
manuelles via journal ADR #47), courbes Hero KPIs.

**Estimation** : 0.5 jour.

---

### #11 — Popup explicative du KPI Delta tech avec mini-chart real vs shadow

**Pourquoi reporté** : le KPI Delta tech est un chiffre nu. Un
visiteur qui veut visualiser la divergence n'a pas d'accès direct à
l'historique courbe.

**Cible** : picto `(i)` à côté du libellé « Delta tech ». Au clic,
popup centrée : texte court explicatif + mini-chart 400×140 px
(60 derniers jours, real solide vs shadow pointillé) + bouton
Fermer.

**Pré-requis partiel** : la mutualisation `ISensorHistory<T>` livrée
en E6 (ADR #53) couvre déjà le besoin de ring-buffer pour les
mesures. Pour les KPIs real/shadow, ring-buffer similaire à
construire dans `SimulationRunner`.

**Estimation** : 4 h.

---

### #17 — Réalisme avancé des capteurs faune

**Pourquoi reporté** : pour le MVP, le piège photo et le capteur
acoustique partagent un profil de bruit identique et statique
(σ = 0.20 / √fauna). Suffisant pour le MVP mais insuffisant pour
simuler les vraies limites de chaque technologie.

**Cibles** :

- Signal acoustique dégradé par la météo (vent, pluie). Pré-requis :
  saisonnalité météo E2 — désormais disponible.
- Piège photo moins efficace en bocage très dense.
- Biais saisonniers (oiseaux vocaux au printemps, amphibiens
  détectables en période humide).
- Score de confiance affiché dans l'UI (intervalle 95 %).

**Pré-requis** : E2 livré (saisonnalité), E6 livré (panneau
inspection capteurs où afficher l'intervalle de confiance).

**Estimation** : 1 jour.

---

### #18 — Recalibrer `MaintenanceCost` selon le référentiel AFAC 2024

**Pourquoi reporté** : la valeur actuelle de 1 €/m/an est défendable
comme coût out-of-pocket mais 3-5× inférieure aux références
sectorielles (Réseau Haies 2024 : 3,69 €/ml gestion durable,
amendement Sénat nov. 2025 : 4,5 €/ml).

**Cible** : rendre le taux paramétrable dans `ScenarioContext` (slider
« mode d'entretien » de auto-réalisation 1 €/ml à prestation 5 €/ml).
Lier à l'item E5 capital (ADR #50) puisque les deux touchent au
modèle économique.

**Estimation** : 0.5 jour.

---

### #19 — Reformuler la croissance des haies comme proxy explicite

**Pourquoi reporté** : `AnnualGrowthMetersPerHectare = 0.5` est
sémantiquement ambigu (densification fonctionnelle, pas allongement
linéaire). Fourchette AFAC régénération 0.2-0.4 m/ha/an suggère que
0.5 est dans le haut de fourchette.

**Cible** :

- Renommer en `AnnualDensificationProxyMetersPerHectare` ou XML doc
  clair.
- Recalibrer sur 0.2-0.4 si arbitré par agronome.
- Documenter dans `DECISIONS.md` la distinction.

**Estimation** : 0.3 jour + arbitrage agronome.

---

### #20 — Recommandations préventives (anticipatives)

**Pourquoi reporté** : les recommandations du MVP sont toutes
réactives — un seuil est franchi, on alerte. Un DT de support à la
décision propose aussi des recommandations anticipatives, basées
sur des tendances détectées avant le franchissement de seuil.

**Architecture cible** :

- `TrendDetector` en Couche 2, ring-buffer de 60 jours par variable
  surveillée + pente glissante.
- `TrendDetectedEvent` (variable observée, horizon prédit,
  confiance).
- Mappings préventifs candidats : tendance nappe, anomalie acoustique
  en formation, saisonnalité défavorable prévue (utilise E2 livré).

**Pré-requis** : ring-buffer par variable (peut réutiliser
`ISensorHistory<T>` livré en E6).

**Estimation** : 1-1.5 jour.

---

### #21 — Levier diversification des cultures

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
- Effet biodiversité : couplage avec `RC_FaunaFactor*` livré en E5.
- UI : slider continu « Diversification de l'assolement ».

**Estimation** : 1 jour.

---

### #22 — Événement échec de plantation

**Pourquoi reporté** : 30-50 % des plants meurent les 3 premières
années (sécheresse, broutage, défaut entretien). Reco PlantHedges
sans aléa sous-estime le coût et risque réel. Rendrait l'arbitrage
plus honnête.

**Architecture cible** :

- `PlantingCohort` (date, magnitude, état vivant/échoué, conditions
  hydriques).
- `PlantingMortalityRule` calcule la fraction de mortalité.
- `PlantingFailureEvent` si > 30 % mortalité.
- `CompletePlantingRecommendation` : compléter la plantation.

**Pré-requis** :

- E5 livré (capital + horizon — sans coût représenté, l'échec n'a
  pas de poids économique).
- #18 (MaintenanceCost recalibré) — pour que le coût de l'entretien
  renforcé soit visible.

**Estimation** : 1-1.5 jour.

---

### #23 — Gestion de la mare (double usage piézomètre + événement + reco)

**Pourquoi reporté** : la mare est présente visuellement et citée
dans les sources (amphibiens, OFB / RMT Zones humides) mais aucun
événement ni reco ne lui est dédié.

**Cible** :

- Extension logique du piézomètre : seconde variable observée
  `PondWaterLevelMeters`.
- `PondDynamicsRule` (forte sensibilité évapotranspiration estivale —
  désormais cohérent avec E2 saisonnalité).
- `PondDryingOutEvent` si < 0.2 m sur 14 jours consécutifs.
- `PondMaintenanceRecommendation`.
- Effet biodiversité (composante amphibiens isolée — couplable avec
  `RC_FaunaFactor*` livré en E5).
- Effet visuel : sprite mare modulé par `PondWaterLevelMeters`
  (extension du `S_Pond` actuel).

**Pré-requis** : E2 livré (saisonnalité débloque la dynamique
évaporation), idéalement E5 livré (pour isoler proprement la
composante amphibiens).

**Estimation** : 1 jour.

---

## 5. Nouveaux items issus du recadrage 2026-05-28

### #24 — Cadre santé végétale complet

**Pourquoi backlog** : remplace les anciens items #14 et #16
(couplage chalara, détection chalara), supprimés par la purge totale
chalara (ADR #46). Réintroduction d'une seule maladie isolée n'est
pas envisagée. Soit on remet un écosystème santé végétale complet,
soit rien.

**Cible** : modélisation cohérente d'une catégorie pathologies +
ravageurs sur les 3 cultures et essences du modèle :

- **Frêne** : chalara fraxinea (capteur drone NDVI ou enquête
  terrain phénologique).
- **Blé tendre** : rouille brune, septoriose (drone NDVI +
  observation).
- **Colza** : sclérotinia (observation phénologique).
- **Chêne / haies** : processionnaire chêne (piège à phéromones).

Avec :

- Capteurs adaptés à chaque pathogène (le piège photo IR ne convient
  pas — sémantique correcte).
- Événements détectables.
- Recommandations algorithmiques associées (rotation, traitements,
  élagage sanitaire).

**Pré-requis** : item #25 (phénologie cultures) — sans phénologie,
les maladies cultures n'ont pas de fenêtre temporelle réaliste.

**Garde-fou** : à ne pas réintroduire item par item — soit on remet
tout l'écosystème santé végétale d'un coup, soit rien (conforme
CLAUDE.md §17).

**Estimation** : 2-3 jours.

---

### #25 — Phénologie cultures (semis, dormance, récolte)

**Pourquoi backlog** : extrait de l'ancien item #12 (saisonnalité +
phénologie). La saisonnalité météo est livrée en E2 (ADR #52), mais
la phénologie cultures (semis, dormance, récolte, GDD, fenêtre
stress hydrique reproductive) reste un chantier post-MVP.

**Cible** :

- `GrowingDegreeDays` (somme T° base 6 °C depuis semis) en variable
  d'état dérivée de `CurrentWeather.TemperatureCelsius` livré en E2.
- Fenêtre semis (jour 280 ≈ octobre pour blé d'hiver) et fenêtre
  récolte (cumul GDD seuil).
- `CropYieldDynamicsRule` reconnaît les phases : croissance active
  vs dormance vs récolte (drop à 0 puis re-build).
- Nouvel événement « stress hydrique en phase reproductive » + reco
  associée.

**Calibration** : INRAE échelle BBCH blé, ARVALIS Eure-et-Loir.

**Pré-requis** : E2 livré (saisonnalité météo).

**Estimation** : 1.5-2 jours.

---

### #26 — Crises saisonnières manuelles (canicule, inondation)

**Pourquoi backlog** : déclenchables manuellement par l'utilisateur
dans la section simulation, avec effets cascade visuels et
mécaniques sur le modèle. Hors MVP.

**Cible** :

- Bouton « Déclencher une crise » dans la section simulation (UI
  Toolkit).
- 2 types de crises : canicule (pic T° prolongé 7-14 jours),
  inondation (pic précip + remontée nappe brutale).
- Effets visuels associés (couleur ciel, prairie). À coupler avec
  item #27.
- Effets mécaniques sur les variables d'état du modèle (cohérents
  avec les règles biophysiques existantes).

**Pré-requis** : E2 livré (saisonnalité), idéalement #27 livré
(effets visuels saisonniers de base).

**Estimation** : 1 jour.

---

### #27 — Effets visuels saisonniers (ciel, prairie)

**Pourquoi backlog** : modulation visuelle du ciel et de la prairie
selon le mois courant et les conditions météo journalières. Hors
MVP.

**Cible** :

- Shader `SG_Sky` : extension pour moduler la couleur selon la T°
  saisonnière (ciel d'hiver pâle/bleu, ciel d'été chaud).
- Shader `S_Meadow` : extension pour moduler la teinte selon la T°
  + humidité (vert frais printemps, jauni été sec).

**Garde-fou critique** : ces effets DOIVENT être dérivés du modèle
(T°, humidité) et non du mois en tant que tel. Le mois n'est pas
une variable mesurée — la T° et l'humidité le sont. Conforme
CLAUDE.md §9 primauté du capteur.

**Pré-requis** : E2 livré.

**Estimation** : 0.5-1 jour par shader.

---

### #28 — 4ème facteur biodiv Diversité paysage

**Pourquoi backlog** : extrait de l'ancien item #15 (refonte biodiv).
La partie 3 facteurs exposés (habitat, eau, intrants) est livrée
en E5 (ADR #51). Le 4ème facteur Diversité paysage reste post-MVP.

**Cible** :

- Nouveau facteur `LandscapeDiversityFactor` calculé Shannon-like
  depuis les % prairies permanentes et la diversité des cultures.
- Nouveaux sliders scenario : `GrasslandPercent` (0-100 %) et
  `CropDiversityIndex` (1-5).
- Recalibration des pondérations à 4 facteurs.
- Affichage 4ème sous-indicateur dans onglet Biodiv (extension du
  binding livré en E6).

**Bénéfice** : courbes de réponse plus fines par espèce visible
(`FaunaPool` livré en E4).

**Pré-requis** : E5 livré (3 facteurs déjà exposés), E6 livré
(onglet Biodiv finalisé).

**Estimation** : 4-6 h.

---

## 6. Liens cross-document

- Items basculés MVP : voir `docs/ROADMAP.md` (chantiers E1-E7) et
  `docs/DECISIONS.md` (ADRs #45 à #56).
- Item livré #5 : sortie naturelle de la sub-étape 9β.
- Items #4, #6 (effets visuels) : prolongement post-MVP du polish
  visuel.
- Items #7 (anims UI), #10 (SessionReporter), #11 (chart real-shadow)
  : polish d'expérience post-MVP.
- Items #17-#19 (recalibrations capteurs/coûts/proxy) : raffinement
  scientifique post-MVP.
- Items #20-#23 : extensions logiques (recos préventives,
  diversification, échec plantation, mare) post-MVP.
- Items #24-#28 : nouveaux post-recadrage du 2026-05-28 (santé
  végétale complète, phénologie, crises manuelles, effets visuels
  saisonniers, 4ème facteur biodiv).

Tout item ajouté au backlog doit pointer vers la décision ou le
chantier d'origine pour ne pas perdre la traçabilité.
