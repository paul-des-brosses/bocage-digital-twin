# WEBGL_GOTCHAS.md — Pièges WebGL anticipés

11 pièges connus du build WebGL Unity. Pour chacun : cause, solution,
impact si non résolu, comment vérifier. Mettre à jour avec la mention
« rencontré le [date], résolu par [solution réelle] » dès qu'un piège
est concrètement rencontré pendant le développement.

---

## 1. Build size explosé

**Cause** : Unity inclut par défaut tous les assets référencés en
`Resources/`, plus les libs runtime, plus des shaders non utilisés. La
taille peut facilement dépasser 50-100 MB sans précaution.

**Solution**

- IL2CPP + Managed Stripping Level **High**.
- Compression **Brotli** activée dans Player Settings WebGL.
- Pas d'assets dans `Resources/` sauf nécessité absolue (tout passer en
  références directes ou `Addressables` si besoin).
- Shader Stripping aggressive activé.
- Audit régulier via `Build Report Inspector`.

**Impact si non résolu** : time-to-interactive > 30 s, abandon
utilisateur sur la démo portfolio.

**Comment vérifier** : `Window > Analysis > Build Report Inspector` après
chaque build. Cible : < 30 MB compressé.

**Notes terrain** : (à remplir si rencontré)

---

## 2. Garbage collection stutters

**Cause** : allocations fréquentes en hot path (string concat, LINQ,
boxing, `Instantiate`/`Destroy` runtime) déclenchent le GC, qui sur
WebGL provoque des pauses visibles (10-50 ms).

**Solution**

- Aucune allocation par frame en hot path.
- Pas de `string.Format` dans `Update`. Pré-formatter les chaînes ou
  utiliser un `StringBuilder` réutilisé.
- Pas de LINQ dans les boucles chaudes.
- Object pooling pour faune et particules (pas
  d'`Instantiate`/`Destroy` runtime).
- Profiler en mode WebGL Development Build pour repérer les allocs.

**Impact si non résolu** : à-coups visuels visibles à chaque GC,
ressenti "saccadé" malgré 60 FPS moyens.

**Comment vérifier** : Profiler Unity en mode WebGL, onglet GC Alloc
par frame. Cible : 0 B/frame en steady state.

**Notes terrain** : (à remplir si rencontré)

---

## 3. Loss of WebGL context

**Cause** : navigateur libère le contexte WebGL (onglet inactif
prolongé, mémoire système saturée). Au retour sur l'onglet, contexte
perdu, scène noire.

**Solution**

- Activer la gestion `WebGL Context Lost` dans Player Settings (option
  Unity 6).
- Implémenter un handler `WebGLInput.captureAllKeyboardInput = false`
  pour éviter les conflits.
- Recharger les ressources critiques au retour (event JavaScript
  bridgé).

**Impact si non résolu** : utilisateur revient sur l'onglet, scène
noire, doit recharger.

**Comment vérifier** : test manuel : laisser l'onglet inactif 10 min,
revenir, vérifier que la scène redémarre.

**Notes terrain** : (à remplir si rencontré)

---

## 4. Audio défaillant

**Cause** : politique d'autoplay des navigateurs (Chrome notamment)
bloque tout audio avant interaction utilisateur. AudioContext
suspendu jusqu'au premier clic.

**Statut pour ce projet** : **N/A**. Le projet est silencieux par
décision de design (cf `DECISIONS.md` #28). Aucun audio à intégrer.

**Solution si jamais audio ajouté ultérieurement** : initialisation
audio uniquement au premier input utilisateur, jamais au boot.

**Impact** : aucun pour ce projet.

**Comment vérifier** : aucun audio attendu dans la console
JavaScript ; aucun warning `AudioContext`.

**Notes terrain** : N/A.

---

## 5. Performance mobile catastrophique

**Cause** : WebGL sur mobile = GPU intégré faible, mémoire limitée,
shaders lents.

**Statut pour ce projet** : **N/A**. Plateforme desktop only assumée
(cf `DECISIONS.md` #17). Bandeau d'avertissement si fenêtre < 1280 px.

**Solution si support mobile demandé ultérieurement** : pipeline
mobile dédié, réduction shader complexity, downscale render target.

**Impact** : aucun pour ce projet, le bandeau prévient l'utilisateur.

**Comment vérifier** : ouvrir la démo sur mobile, vérifier que le
bandeau s'affiche.

**Notes terrain** : N/A.

---

## 6. Time.deltaTime instable

**Cause** : sur WebGL, le temps réel peut être instable (throttling
onglet inactif, skip de frames). Les calculs basés sur `Time.deltaTime`
peuvent diverger.

**Solution**

- Le temps **simulé** est cadencé par tick, pas par `Time.deltaTime`
  (cf `ARCHITECTURE.md` §5).
- Coroutine de tick basée sur un compteur de temps réel propre, avec
  clamp si delta > 1 s (anti-throttling).
- Animations cosmétiques Couche 5 utilisent `Time.unscaledDeltaTime`
  avec clamp.

**Impact si non résolu** : la simulation peut "sauter" plusieurs jours
quand l'utilisateur revient sur un onglet inactif.

**Comment vérifier** : laisser l'onglet inactif 30 s, revenir, vérifier
que la simulation reprend proprement (pas de saut massif).

**Notes terrain** : (à remplir si rencontré)

---

## 7. Long initial loading

**Cause** : taille du build, taille du heap initial, parsing du
JavaScript Unity loader.

**Solution**

- Build size minimisé (cf piège #1).
- Initial heap size raisonnable (`WebGL > Player Settings > Memory Size`
  → ne pas surdimensionner).
- Loader Unity 6 par défaut (compression Brotli + decompression
  fallback).
- Splash screen léger / écran de chargement stylisé.

**Impact si non résolu** : utilisateur abandonne avant que la démo se
lance.

**Comment vérifier** : Chrome DevTools > Network > test sur connexion
"Fast 3G" simulée. Cible : time-to-interactive < 10 s.

**Notes terrain** : (à remplir si rencontré)

---

## 8. Memory leaks via textures non libérées

**Cause** : textures créées dynamiquement (`new Texture2D`,
`RenderTexture`) non `Destroy()` correctement → fuite mémoire heap
WebGL → crash après quelques minutes.

**Solution**

- Pas de création dynamique de textures sans pooling.
- Si `RenderTexture` nécessaire, allouer une fois au boot et réutiliser.
- Toujours `Object.Destroy(tex)` (pas seulement déréférencer).

**Impact si non résolu** : crash navigateur après 5-15 min de session.

**Comment vérifier** : Profiler Unity > Memory > vérifier que le
nombre de textures reste stable au cours d'une session longue.

**Notes terrain** : (à remplir si rencontré)

---

## 9. Polices SDF mal chargées

**Cause** : TextMesh Pro nécessite des assets SDF pré-générés. Si la
police n'est pas convertie en SDF, ou si la taille de l'atlas dépasse
la limite WebGL → texte invisible ou pixelisé.

**Solution**

- Générer les assets TMP SDF pour EB Garamond et JetBrains Mono via
  `Window > TextMeshPro > Font Asset Creator`.
- Atlas 1024×1024 maximum.
- Inclure les caractères latins étendus (accents français, € symbol,
  etc.).
- Garder les fichiers de police source dans `Assets/_Project/Fonts/`.

**Impact si non résolu** : UI sans texte ou texte illisible en build.

**Comment vérifier** : ouvrir le build WebGL, vérifier que tous les
labels Garamond et chiffres mono s'affichent correctement, y compris
les accents.

**Notes terrain** : (à remplir si rencontré)

---

## 10. Scripts éditeur qui plantent en build

**Cause** : un script dans le scope runtime qui contient
`#if UNITY_EDITOR` mal placé, ou qui référence `UnityEditor.*` hors
guard, casse le build WebGL avec un message d'erreur cryptique.

**Solution**

- Scripts éditeur strictement dans `Assets/_Project/Editor/` (asmdef
  Editor only).
- Aucun `using UnityEditor;` dans un script runtime, même sous
  `#if UNITY_EDITOR`. Si nécessaire, isoler dans un fichier dédié.
- Audit avant build : grep `using UnityEditor` dans tout sauf `Editor/`.

**Impact si non résolu** : build casse, message d'erreur peu explicite.

**Comment vérifier** : faire un build WebGL régulièrement (au moins à
chaque palier de la roadmap). Échec immédiat = piège #10 probable.

**Notes terrain** : (à remplir si rencontré)

---

## 11. Shaders non compatibles WebGL

**Cause** : certaines features Shader Graph (geometry shaders, compute
shaders, texture sampling avancé) ne sont pas supportées en WebGL 2.0.

**Solution**

- Cibler **WebGL 2.0** explicitement dans Player Settings (pas WebGL
  1.0, pas WebGPU).
- Éviter compute shaders, geometry shaders.
- Tester chaque shader Shader Graph en build WebGL dès sa création
  (pas seulement en éditeur).
- 2D Renderer URP par défaut, post-process light (Bloom + Color
  Grading).

**Impact si non résolu** : shaders affichent magenta (shader error) en
build, scène cassée visuellement.

**Comment vérifier** : build WebGL, tester chaque shader visuellement.
Aucun magenta toléré.

**Notes terrain** : (à remplir si rencontré)
