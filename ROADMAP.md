# ROADMAP.md — Plan de production

10 étapes verticales, chacune avec un livrable démontrable. Chaque étape
peut être un point de coupe propre si le scope déborde.

**Statut global** : à faire (projet en bootstrap).

---

## Étape 1 — Bootstrap projet

**Objectif** : repo, documentation et structure de dossiers en place.

**Livrables**

- 9 fichiers de documentation (`README.md`, `CLAUDE.md`, `DECISIONS.md`,
  `ARCHITECTURE.md`, `ROADMAP.md`, `WEBGL_GOTCHAS.md`,
  `ASSETS_LIST.md`, `LICENSE`, `.gitignore`).
- Arborescence de dossiers Unity (vide, avec `.gitkeep`).

**Critère de validation**

- Repo public sur GitHub avec structure visible.
- README rendu correctement sur la page repo.

**Estimation** : 0.5 jour.

**Statut** : à faire.

---

## Étape 2 — Architecture squelette

**Objectif** : projet Unity 6 créé, asmdef en place pour les 5 couches,
scène `Main` avec 7 racines préfixées, bootstrap minimal.

**Livrables**

- Projet Unity 6 LTS configuré (URP 2D, build target WebGL).
- 5 asmdef (`Bocage.SimulationCore`, `Bocage.Sensors`,
  `Bocage.Decision`, `Bocage.Indicators`, `Bocage.Presentation`) avec
  références strictes.
- Scène `Main` avec les 7 racines préfixées `_`.
- `_Bootstrap` MonoBehaviour qui logue "bootstrap OK" via `SimLogger`.
- `SimLogger` à 3 niveaux fonctionnel.
- Player Settings WebGL configurés (IL2CPP, stripping High, Brotli).

**Critère de validation**

- Build WebGL passe en local.
- Hiérarchie de scène conforme.
- Tentative d'ajout d'un `using UnityEngine;` dans la Couche 1 → erreur
  de compilation.

**Estimation** : 1 jour.

**Statut** : à faire.

---

## Étape 3 — Simulation core minimaliste

**Objectif** : Couche 1 fonctionnelle avec un modèle d'écosystème
minimal et quelques règles biophysiques. Tests unitaires en place.

**Livrables**

- `SimulationEngine` avec coroutine de tick (1 tick = 1 jour).
- `EcosystemModel` avec : nappe phréatique, densité haies, météo
  (température, précipitations).
- 3 à 5 `BiophysicalRules` (croissance haies, dynamique nappe, impact
  pluie sur sol).
- `SeededRandom` avec sous-seeds par hash.
- `ScenarioContext` avec presets initiaux.
- `TransitioningParameter<T>` fonctionnel.
- 5 tests EditMode minimum (déterminisme, conservation, dynamique
  nappe).

**Critère de validation**

- Tests passent en EditMode.
- Lancement Play Mode : log de progression du modèle dans la console
  via `SimLogger`.

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Étape 4 — Scène visuelle minimaliste

**Objectif** : scène 2D affiche un paysage statique reconnaissable du
Perche.

**Livrables**

- Sprites background, midground, foreground en place (versions
  provisoires si Nanobanana pas prêt).
- Composition de scène avec ordre de rendu correct.
- Shader sky (gradient ciel) en Shader Graph, paramétrable.
- Caméra orthographique fixe configurée.
- Composition validée en Play Mode.

**Critère de validation**

- Scène lisible, esthétiquement cohérente avec la direction artistique.
- Build WebGL toujours fonctionnel.

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Étape 5 — Liaison simu-visuel + 1er Hero KPI

**Objectif** : démontrer le pipeline complet sur un seul indicateur.

**Livrables**

- ScriptableObject observable `RC_HedgerowDensity`.
- `HedgerowDensityIndicator` (Couche 4) qui lit
  `EcosystemModel.HedgerowDensity` et écrit dans le ScriptableObject.
- `HedgerowDensityBinding` (Couche 5) qui écoute le SO et met à jour un
  texte UI.
- Affichage du KPI à l'écran avec valeur qui évolue en simulation.
- Shader haies (Couche 5) module la couleur des sprites haies en
  fonction du SO.

**Critère de validation**

- Démo : on lance la simu, le KPI bouge, les haies à l'écran réagissent
  visuellement à l'évolution de la densité.

**Estimation** : 1 jour.

**Statut** : à faire.

---

## Étape 6 — UI complète et 5 Hero KPIs

**Objectif** : tableau de bord complet en place avec les 5 Hero KPIs et
les 3 panneaux Niveau B.

**Livrables**

- Layout dark mode complet (Garamond + JetBrains Mono).
- 5 Hero KPIs : densité haies, biodiversité composite, nappe
  phréatique, rentabilité intégrée, delta tech (encore à 0 à ce stade).
- 3 panneaux Niveau B (Biodiversité, Climat & ressources, Économie)
  avec sous-indicateurs.
- Capteurs visibles dans la scène avec sprites.
- Minimap vectorielle avec capteurs positionnés.
- Hover synchronisé minimap ↔ scène (highlight visuel).
- Tooltips Garamond italique sur hover.
- Bandeau d'avertissement si fenêtre < 1280 px.

**Critère de validation**

- Toute l'UI est en place et lisible.
- Build WebGL < 30 MB toujours respecté.
- Démo : 5 KPIs bougent, les 3 panneaux affichent leurs
  sous-indicateurs.

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Étape 7 — Système de presets et casquette Scénario

**Objectif** : l'utilisateur peut régler le contexte scénario.

**Livrables**

- Scenario panel UI avec 4 curseurs (climat, pression agricole,
  contraintes réglementaires, horizon).
- ScriptableObjects de presets dans `Data/ScenarioPresets/`.
- Application des presets via `TransitioningParameter<T>` (interpolation
  7-14 jours simulés).
- Persistance PlayerPrefs de la dernière configuration de presets.
- Boutons play/pause/x1/x10/skip-to-end fonctionnels.

**Critère de validation**

- Démo : modification d'un curseur → transition douce visible dans la
  scène et les KPIs.
- Pause / reprise / vitesses fonctionnent.

**Estimation** : 1 jour.

**Statut** : à faire.

---

## Étape 8 — Système de décisions et casquette Recommandations

**Objectif** : moteur de décision riche en place, recommandations
arbitrables par l'utilisateur, comparaison shadow run fonctionnelle.

**Livrables**

- `EventDetector` (Couche 2) détecte au moins 3 types d'événements
  (chalara, sécheresse prolongée, anomalie acoustique).
- `RecommendationEngine` (Couche 3) produit recommandations à partir
  des événements.
- `OutcomeProjector` avec incertitudes (distributions) et 2 horizons
  (court / long terme).
- `AutoActions` appliquées en real run.
- `DecisionJournal` append-only.
- Decision panel UI avec recommandations à arbitrer (accepter / rejeter).
- `ShadowSimulationRunner` opérationnel.
- Hero KPI "delta tech" calculé et affiché.
- Vue de comparaison real vs shadow.

**Critère de validation**

- Démo : un événement chalara apparaît, une recommandation s'affiche,
  l'utilisateur arbitre, l'effet sur les KPIs diverge entre real et
  shadow.
- Outcomes projetés visibles avec barres d'incertitude.

**Estimation** : 2 jours.

**Statut** : à faire.

---

## Étape 9 — Tous les effets visuels et faune

**Objectif** : scène vivante avec tous les sprites finaux, modulation
visuelle pilotée par le modèle.

**Livrables**

- Tous les sprites finaux générés via Nanobanana et post-traités.
- Faune en pool (hirondelle, chouette, busard, héron, amphibien) avec
  patterns d'animation simples.
- Densité de faune pilotée par l'index de biodiversité.
- Shader haies, mare, prairie pilotés par variables modèle (humidité,
  healthT, niveau d'eau).
- Effets Niveau 3 : modulation `healthT` sur faune et haies.
- Particules Unity (feuilles dérivantes, poussières dans la lumière).

**Critère de validation**

- Démo : scène riche, faune pool tourne, les effets visuels suivent
  les variables du modèle (vérifié par audit primauté du capteur).

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Étape 10 — Polish, optimisation, déploiement final

**Objectif** : version portfolio livrable, déployée sur GitHub Pages.

**Livrables**

- Workflow GitHub Actions (`game-ci/unity-builder`) qui build et
  déploie sur la branche `gh-pages`.
- Build WebGL final < 30 MB (compressé Brotli).
- Time-to-interactive < 10 s vérifié.
- 60 FPS stable vérifié.
- Polish UI final (alignements, marges, hovers).
- README finalisé avec démo link, GIF hero, screenshots.
- `SessionReporter` opérationnel et accessible depuis l'UI.
- Audit final : aucune violation de la primauté du capteur.

**Critère de validation**

- Démo accessible publiquement sur `https://<user>.github.io/<repo>/`.
- README avec liens vivants.
- Build CI vert.

**Estimation** : 1.5 jour.

**Statut** : à faire.

---

## Total et marges

- Somme brute : 13 jours-équivalents IA-assistés.
- Marge réaliste × 1.3 : **~17 jours**.

---

## Stratégie de coupe en cas de dépassement

Ordre de coupe (du plus acceptable au plus douloureux) — cf `CLAUDE.md`
§17 :

1. Implémentation décision **moyenne** au lieu de riche (réduire
   incertitudes, un seul horizon).
2. Suppression des effets visuels Niveau 3 (modulation `healthT` sur
   faune et haies).
3. Réduction tests unitaires de 5-10 à 3-5.
4. Réduction sprites uniques de 15 à 10 (fusion de variantes).
5. **NE PAS COUPER** : architecture 5 couches, organisation Git,
   cohérence du pipeline assets, polish UI final.

---

## Règle d'or

À **70 % du temps écoulé**, le projet doit être à **~85 % fonctionnel**.

Sinon, déclencher la stratégie de coupe immédiatement, dans l'ordre
ci-dessus. Ne pas espérer rattraper en sprint final.

Vérification recommandée à la fin de l'Étape 7 (~70 % de la roadmap) :
si l'UI complète n'est pas en place, couper.
