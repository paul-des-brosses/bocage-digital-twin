# CLAUDE.md — Spécification opérationnelle

Document de référence pour Claude Code en phase de production. À lire comme
un cahier des charges d'ingénieur lead.

---

## 1. Contexte

- **Auteur** : étudiant M1 ESILV (Creative Technology), stage R&D Ardanti.
- **Finalité** : construction d'un portfolio GitHub public démontrant la
  maîtrise d'Unity WebGL, de la simulation temps réel et d'une architecture
  logicielle propre.
- **Sujet** : digital twin d'un bocage normand percheron instrumenté.
  Mosaïque agro-forestière (haies, prairie, bosquet, mare, arbres têtards),
  site fictif mais plausible ancré dans le Perche normand.
- **Thèse** : la simulation teste honnêtement la convergence éco/écolo via
  instrumentation et monétisation des services écosystémiques, sans
  postuler le résultat. La technologie de capteurs et la décision
  algorithmique aident l'humain à mieux gérer un paysage co-construit.
- **Cibles techniques** : démonstrateur portfolio, Unity 6 LTS, build
  WebGL déployé sur GitHub Pages via GitHub Actions.

---

## 2. Division du travail Claude Code / utilisateur

### Claude Code fait

- Code C# de toutes les couches (01 à 05).
- ScriptableObjects (définitions et instances par défaut).
- Shader Graph (sky, prairie, hedgerows, pond).
- Prefabs Unity (assemblage et configuration).
- Tests unitaires EditMode.
- Scripts d'audit (vérification des asmdef, conformité au principe de
  primauté du capteur, etc.).
- Scripts de post-traitement Python pour les assets (palette quantization,
  alpha cleanup, normalisation).
- Configuration des Assembly Definitions (asmdef).
- Configuration GitHub Actions (workflow CI/CD WebGL).
- Génération des fichiers de configuration Unity (ProjectSettings textuel
  quand pertinent).
- Suggestions de messages de commit Conventional Commits aux moments
  opportuns.

### L'utilisateur fait

- Décisions de design (architecture, choix produit, scope).
- Validation finale de chaque palier.
- Exécution de toutes les commandes Git (commit, push, branches, merges).
- Intégration manuelle entre modules dans l'éditeur Unity.
- Debug visuel en jeu (Play Mode, runs WebGL).
- Calibration scientifique des paramètres (Solagro, INRAE, Efese, MAEC,
  PNR du Perche).
- Polish visuel final (réglages fins, ressentis, ajustements de palette).
- Configuration manuelle Unity (licences, secrets GitHub, paramétrages
  éditeur ponctuels qui ne s'automatisent pas raisonnablement).
- Génération des sprites IA (Nanobanana, ip-adapter) et validation
  visuelle de chaque sortie.
- Dessin du plan vectoriel de la minimap (Inkscape ou Figma).

---

## 3. Règle pédagogique de Claude Code envers l'utilisateur

L'utilisateur n'est pas un ingénieur Unity senior ; il apprend en pilotant.
Quand Claude Code demande une action manuelle, il doit :

1. **Énoncer clairement l'action attendue** avec le résultat ciblé.
2. **Donner le chemin exact** dans Unity (`Edit > Project Settings >
   Player > ...`) ou dans le système de fichiers.
3. **Préciser pourquoi cette action ne peut pas être automatisée** (limite
   de l'éditeur, sécurité, choix de design, etc.).
4. **Décrire le résultat visuel ou comportemental attendu** pour que
   l'utilisateur puisse valider.
5. **Anticiper les pièges courants** (cases à cocher faciles à oublier,
   ordre des opérations, etc.).
6. **Proposer une vérification simple post-action** (un état attendu, une
   ligne de log, un comportement observable).

---

## 4. Workflow Git

L'utilisateur gère seul toutes les commandes Git. Claude Code ne fait
**aucun** commit, push, branch ou merge. Claude Code propose des messages
de commit au format Conventional Commits aux moments opportuns :

- Fin de palier de la roadmap.
- Refactor cohérent terminé.
- Étape testable et indépendante terminée.

### Format

```
<type>(<scope>): <description>
```

### Types autorisés

`feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `perf`, `style`.

### Scopes autorisés (alignés sur les couches et domaines)

`simulation`, `sensors`, `decision`, `indicators`, `presentation`, `ui`,
`data`, `build`, `readme`, `tests`, `repo`.

### Exemple

```
feat(simulation): add deterministic seeded random with sub-seed derivation
```

---

## 5. Architecture en 5 couches : règles strictes

### 5.1 Couche 01_SimulationCore

- **Pure C#** : asmdef sans aucune référence à `UnityEngine` ni à
  `UnityEditor`.
- Logique biophysique du bocage (modèle d'écosystème, règles de
  dynamique).
- Doit être entièrement testable en EditMode sans dépendance Unity.
- Pas d'I/O. Pas de logging direct. Pas de chrono système.

### 5.2 Couche 02_Sensors

- Transforme l'état simulé en mesures bruitées (modèle de capteur).
- Détecteurs d'événements (chalara détecté, sécheresse prolongée, etc.).
- Référence Couche 1, jamais l'inverse.

### 5.3 Couche 03_Decision

- Moteur de recommandations à partir des événements détectés.
- Outcome projector avec incertitudes (distributions, horizons multiples).
- Journal des décisions utilisateur et algorithmiques.
- Référence Couches 1 et 2.

### 5.4 Couche 04_Indicators

- Agrégation en Hero KPIs (5 indicateurs principaux).
- Panneaux Niveau B (Biodiversité, Climat & ressources, Économie).
- Simulation fantôme (shadow run) sans actions tech, mêmes seeds.
- Reporter de session.
- Référence Couches 1, 2, 3.

### 5.5 Couche 05_Presentation

- MonoBehaviours Unity, UI Toolkit ou uGUI, shaders, bindings.
- Lecture seule des ScriptableObjects observables produits par les
  couches inférieures.
- Référence toutes les couches inférieures, mais ne pousse jamais d'état
  métier vers elles ; transmet seulement les inputs utilisateur via le
  `ScenarioContext`.

### 5.6 Règle invariante

**Les couches inférieures ne dépendent jamais des couches supérieures.**
La Couche 1 ne référence aucun namespace `UnityEngine`. Les asmdef sont
configurés pour rendre toute violation de cette règle impossible à la
compilation.

---

## 6. Règles de code dures

- **Aucun appel à `UnityEngine.Random.*`** dans les Couches 1-2-3-4.
  Utiliser `SeededRandom` (sous-seeds dérivés par hash du seed maître).
- **Aucun `GameObject.Find` ou `FindObjectsOfType` runtime.** Références
  via Inspector ou ScriptableObjects.
- **Object pooling pour faune et particules.** Pas
  d'`Instantiate`/`Destroy` runtime hors bootstrap.
- **Aucune allocation par frame en hot path.** Pas de `string.Format`
  dans `Update`, pas de boxing, pas de LINQ dans les boucles chaudes.
- **Coroutines plutôt que `async/await`** (compatibilité WebGL et
  contrôle déterministe).
- **Aucune Reflection runtime.**
- **Tous les ScriptableObjects de runtime suivent le pattern observable**
  (event `OnChanged` exposé, mutation via méthodes dédiées).
- **EventBus statique** pour les événements ponctuels (pas pour l'état
  persistant).
- **Pas de `Debug.Log` direct.** Passer par `SimLogger` à 3 niveaux :
  `DebugLog` (développeur), `SimulationLog` (événements de modèle),
  `UserActionLog` (actions utilisateur).

---

## 7. Performance WebGL : règles dès le code

- **IL2CPP**, Managed Stripping Level **High**.
- **Compression Brotli** pour le build WebGL.
- **ASTC** ou **Crunched DXT** pour les textures.
- **2D Renderer URP**, post-processing minimal (Bloom léger + Color
  Grading léger uniquement).
- **Pas de MSAA**, **pas de Depth of Field**, **pas de threads**.
- **Cibles de build** :
  - Taille build < 30 MB (compressé).
  - Time-to-interactive < 10 s sur connexion résidentielle moyenne.
  - 60 FPS stable sur desktop standard.

---

## 8. Règles d'organisation

- Structure `Assets/_Project/` numérotée 01 à 05 par couche.
- Un asmdef par couche, références strictes (Couche N ne voit que les
  couches M < N).
- ScriptableObjects observables stockés dans `Data/RuntimeContainers/`.
- Une seule scène Unity (`Main`).
- Hiérarchie de scène à 7 racines préfixées `_` :
  - `_Bootstrap`
  - `_Camera`
  - `_Scene_Visual`
  - `_Scene_Overlays`
  - `_UI_Canvas`
  - `_Audio` (réservé, vide pour ce projet — voir §10)
  - `_Debug`

---

## 9. Principe absolu : primauté du capteur

**Aucun élément visuel n'est piloté par le calendrier ou par une logique
scénique.** Toute variation visuelle est dérivée d'une mesure ou d'une
variable du modèle de simulation, traçable jusqu'à un capteur ou un
calcul du modèle.

Si toi, Claude Code, te surprends à coder un effet « saisonnier » ou
« d'ambiance » purement décoratif (feuilles d'automne au mois d'octobre,
neige en hiver scénarisée, etc.), tu t'arrêtes et tu signales à
l'utilisateur qu'il faut dériver l'effet d'une mesure (température
moyenne, humidité du sol, healthT d'un agent, etc.).

Cette règle est non négociable : c'est ce qui distingue un digital twin
d'un jeu vidéo.

---

## 10. Audio

**Aucun audio dans ce projet.** Aucune musique, aucun bruitage, aucun son
d'ambiance, aucun son de feedback UI. Le projet est silencieux. La racine
`_Audio` dans la hiérarchie est conservée vide pour cohérence
structurelle.

---

## 11. Mode dark UI

Toute l'interface est en dark mode :

- **Fond général** : sombre désaturé, gris-anthracite chaud.
- **Textes** : crème / ivoire.
- **Accents** : désaturés (vert bouteille, ocre, rouge terre, ardoise).
- **Typographies** :
  - **Garamond** (EB Garamond) pour titres et labels.
  - **Mono élégante** (JetBrains Mono ou IBM Plex Mono) pour les valeurs
    chiffrées.

---

## 12. Plateforme

Desktop only assumé. Pas de responsive mobile, pas de gestion tactile.
**Bandeau de message si fenêtre < 1280 px** indiquant que l'expérience
n'est pas optimisée et invitant à élargir.

---

## 13. Gestion des sprites

Voir `ASSETS_LIST.md` pour la liste exhaustive des sprites attendus.

Pipeline :

1. Génération via **Nanobanana** avec **ip-adapter style reference**.
2. Post-traitement Python (palette quantization sur palette Perche, alpha
   cleanup, normalisation des dimensions).
3. Placement dans `Assets/_Project/05_Presentation/Scene/Sprites/` dans
   le sous-dossier thématique adéquat.
4. Sources brutes archivées dans `Sprites/Source/`.

---

## 14. Tests

Tests unitaires en **EditMode** sur la Couche 1, **obligatoires**.

- **Cible** : 5 à 10 tests minimum.
- **Couverture attendue** :
  - Déterminisme (même seed + mêmes inputs → même état).
  - Quelques règles biophysiques clés (croissance haie, dynamique nappe,
    impact sécheresse, etc.).
  - Invariance de variables clés (bornes, conservation).
  - Cohérence simulation fantôme (mêmes seeds → divergence
    exclusivement due aux actions tech).

---

## 15. Configuration de la simulation

- **Tick rate** : 1 tick = 1 jour simulé.
- **Vitesses utilisateur** : x1 (1 tick/seconde), x10 (10 ticks/seconde),
  bouton « skip to end » au-delà.
- **Seed** : seed maître au démarrage, sous-seeds dérivés par hash pour
  chaque sous-système (météo, faune, capteurs, événements).
- **Simulation fantôme** : run parallèle, mêmes seeds et inputs, sans
  application des actions tech (`applyTechActions = false`).
- **Transitions de paramètres** : interpolation sur 7-14 jours simulés
  via `TransitioningParameter<T>`. Aucune mutation abrupte.

---

## 16. Persistance

**PlayerPrefs minimal** : dernière configuration de presets et vitesse
choisie. Rien d'autre. Pas de sauvegarde de session, pas de profils, pas
de cloud sync.

---

## 17. Stratégie de coupe en cas de dépassement

Si le scope déborde, ordre de coupe (du plus acceptable au plus
douloureux) :

1. Implémentation décision **moyenne** au lieu de riche (moins
   d'incertitudes, horizons réduits).
2. Suppression des effets visuels Niveau 3 (modulation `healthT` sur
   faune et haies).
3. Réduction tests unitaires de 5-10 à 3-5.
4. Réduction sprites uniques de 15 à 10 (fusion de variantes).
5. **NE PAS COUPER** : architecture 5 couches, organisation Git,
   cohérence du pipeline assets, polish UI final.

---

## 18. En cas de doute

Si une décision technique semble manquer dans cette spécification,
consulter dans l'ordre :

1. `DECISIONS.md` (décisions de design déjà tranchées).
2. `ARCHITECTURE.md` (détails architecturaux).
3. Demander explicitement à l'utilisateur.

**Ne pas combler de soi-même.** Une décision non documentée est une
décision à prendre, pas une décision à improviser.
