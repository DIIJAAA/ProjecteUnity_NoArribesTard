# PROJECTE UNITY
# NO ARRIBES TARD

## INDEX
1. INTRODUCCIO
   1.1 Objectius del projecte
   1.2 Tecnologies i eines utilitzades
2. ESTRUCTURA DEL PROJECTE
   2.1 Organitzacio de carpetes
   2.2 Arquitectura del codi
3. FUNCIONALITATS IMPLEMENTADES
   3.1 Logica principal del joc (nivells i decisions)
   3.2 Sistema de loop infinit i teletransport
   3.3 Sistema d'anomalies
   3.4 Sistema de jugador i controls
   3.5 Sistema de pausa i guardat de partida
   3.6 Sistema de temps i record
   3.7 Sistema d'audio
4. ESCENES DEL JOC
   4.1 MenuInici
   4.2 Passadis
   4.3 GameOver
5. PLAYERPREFS I PERSISTENCIA
   5.1 Dades que es guarden
   5.2 Flux de guardat i carrega
6. PROBLEMES TROBATS I SOLUCIONS
7. POSSIBLES MILLORES FUTURES
8. CONCLUSIONS FINALS
9. WEBGRAFIA I RECURSOS UTILITZATS

---

## 1. INTRODUCCIO
No Arribes Tard es un joc en primera persona inspirat en la logica de deteccio d'anomalies i recorregut en bucle.
El jugador ha de memoritzar l'estat base del passadis i decidir correctament si hi ha anomalia o no.

### 1.1 Objectius del projecte
- Implementar un bucle jugable fluid en primera persona.
- Implementar anomalies aleatories (normals i subtils) per fer el joc rejugable.
- Implementar progressio per nivells amb condicio de victoria.
- Implementar menus separats (inici, pausa, game over).
- Implementar persistencia amb PlayerPrefs per continuar partida i guardar record de temps.

### 1.2 Tecnologies i eines utilitzades
- Unity 6.2 (6000.2.8f1)
- C# (scripts MonoBehaviour)
- TextMeshPro per textos UI i cartells
- CharacterController per moviment del jugador
- SceneManager per canvi d'escenes
- PlayerPrefs per persistencia local

---

## 2. ESTRUCTURA DEL PROJECTE

### 2.1 Organitzacio de carpetes
Estructura principal usada:
- `Assets/Scenes`
  - `MenuInici.unity`
  - `Passadis.unity`
  - `GameOver.unity`
- `Assets/Scripts`
  - `GameManager.cs`
  - `AnomalyManager.cs`
  - `CorridorLoopController.cs`
  - `PlayerController.cs`
  - `ZonaTrigger.cs`
  - `MainMenuController.cs`
  - `PauseOverlayController.cs`
  - `GameOverController.cs`
- `Assets/Prefabs` (elements reutilitzables)
- `Assets/MyAsset` (assets importats: imatges, sons, etc.)

### 2.2 Arquitectura del codi
El projecte esta centrat en un controlador principal:
- `GameManager` (singleton persistent amb `DontDestroyOnLoad`) coordina estat de joc, escenes, progressio, guardat i record.

Gestors auxiliars:
- `AnomalyManager` gestiona descoberta d'objectes, activacio i restauracio d'anomalies.
- `CorridorLoopController` aplica teletransport i orientacio canonica per mantenir el loop.
- `PlayerController` gestiona moviment, camera, cursor i passos.
- `ZonaTrigger` detecta entrada del jugador a la zona d'inici o final.

UI per escena:
- `MainMenuController` (botons comencar/continuar/sortir + audio menu).
- `PauseOverlayController` (reprendre, guardar i sortir).
- `GameOverController` (temps de partida, record i retorn a menu).

---

## 3. FUNCIONALITATS IMPLEMENTADES

### 3.1 Logica principal del joc (nivells i decisions)
Implementada a `GameManager.cs`:
- El joc comenca a `nivellActual = 0`.
- En nivell 0 no hi pot haver anomalies (`PrepararRonda` les desactiva sempre).
- El cartell de nivell 0 mostra exactament:
  - `NIVELL 0.`
  - `MEMORITZA ELS DETALLS!`
- A partir del nivell 1, cada ronda pot tenir anomalia o no.
- Regla de decisio:
  - Si hi ha anomalia: la resposta correcta es tornar enrere (`ZonaInici`).
  - Si no hi ha anomalia: la resposta correcta es continuar endavant (`ZonaFinal`).
- Si encerta: `nivellActual++`.
- Si falla: el joc torna a `nivellActual = 0`.
- Victoria: quan el nivell supera `nivellVictoria` (valor actual 4).

### 3.2 Sistema de loop infinit i teletransport
Implementat a `CorridorLoopController.cs`:
- El sistema usa dues zones de trigger (`ZonaInici` i `ZonaFinal`).
- En mode actual (`sempreReapareixerAInici = true`), qualsevol entrada reapareix a la zona d'inici per conservar direccio canonica.
- Es calcula una nova posicio respectant offset local i limits del trigger.
- S'aplica rotacio canonica per evitar inversions del passadis.
- Te cooldown de moviment (`movimentCooldown`) per evitar teleports multiples seguits.
- Reprodueix so de teleport opcional amb variacio lleu de pitch.

### 3.3 Sistema d'anomalies
Implementat a `AnomalyManager.cs`.

Logica general:
- Detecta objectes candidats de forma automatica (portes, llums, cartells, botons, radiadors, pilars).
- Per cada ronda:
  - Reseteja estat previ (`DesactivarTot`).
  - Si nivell <= 0: no activa cap anomalia.
  - Si nivell > 0: aplica probabilitat (`probabilitatAnomalia`, actual 0.85).
  - Tria tipus aleatori disponible.
  - Evita repeticio exacta de tipus/index consecutius (si hi ha alternatives).

#### Llistat complet d'anomalies implementades (15)

Anomalies normals (5):
1. `NormalExtraDoors`
   - Duplica una porta base dues vegades (`+2.2` i `-2.2` en Z local).
2. `NormalLightFlicker`
   - Parpelleig amb coroutine: canvia intensitat i on/off rapidament.
3. `NormalLightColorStrong`
   - Canvi fort de color de llum (vermell, verd, blau o magenta) + pujada d'intensitat.
4. `NormalRadiatorColor`
   - Canvi de color d'un radiador a variants destacades.
5. `NormalLightsOff`
   - Apaga 2 o 3 llums properes a una llum central.

Anomalies subtils (10):
6. `SubtleWcSign`
   - Variant 1: cartell de WC en mirall (escala X negativa).
   - Variant 2: text canviat a "WC HOMES".
7. `SubtleElevatorButtons`
   - Variant 1: desapareix un boto.
   - Variant 2: es duplica un boto proper.
8. `SubtlePosterColor`
   - Canvi subtil de color d'un cartell del panell.
9. `SubtleExtraPoster`
   - Duplica un cartell (paper) en posicio propera.
10. `SubtleLightTint`
   - Canvi subtil de to de llum (mes calid/fred) amb intensitat lleu.
11. `SubtleMissingPillar`
   - Desactiva un pilar.
12. `SubtleDoorAtEnd`
   - Afegeix una porta addicional en direccio de la zona final.
13. `SubtleMissingLight`
   - Apaga una llum concreta.
14. `SubtleWeirdSoundMiddle`
   - Genera so procedural 3D estrany al mig del passadis (loop).
15. `SubtleDoorColor`
   - Canvi subtil del color d'un panell de porta.

Restauracio:
- Totes les anomalies guarden estat original (colors, visibilitat, llums, textos, etc.).
- En cada canvi de ronda es restaura tot per evitar acumulacio d'efectes.

### 3.4 Sistema de jugador i controls
Implementat a `PlayerController.cs`:
- Moviment WASD amb `CharacterController`.
- Correr amb `LeftShift`.
- Mirar amb ratoli (yaw al player, pitch a la camera).
- Gravetat configurable.
- Blocatge/desblocatge d'input i cursor segons estat (joc/pausa/menu).
- So de passos amb `AudioSource` i clips aleatoris.
- Funcions de teleport segur (desactiva temporalment `CharacterController`).

### 3.5 Sistema de pausa i guardat de partida
Implementat a `GameManager.cs` + `PauseOverlayController.cs`:
- `Esc` durant partida obre overlay de pausa.
- Opcions:
  - Reprendre partida.
  - Guardar i sortir al menu.
- En guardar, es fa snapshot de l'estat actual.

### 3.6 Sistema de temps i record
Implementat a `GameManager.cs` + `GameOverController.cs`:
- El temps de partida (`tempsPartida`) s'acumula en estat `Playing`.
- En victoria, es guarda `tempsUltimaPartida`.
- Es compara amb `tempsRecord`; si es millor, s'actualitza record.
- A `GameOver` es mostren:
  - Temps d'aquesta partida.
  - Record de temps.

### 3.7 Sistema d'audio
Audio implementat en diversos punts:
- Menu principal:
  - musica de menu + ambient en bucle (`MainMenuController`).
- Passadis:
  - passos del jugador (`PlayerController`).
  - so de teleport (`CorridorLoopController`).
  - so estrany procedural com anomalia (`AnomalyManager`).
- Game over:
  - so final opcional (`GameOverController`).

---

## 4. ESCENES DEL JOC

### 4.1 MenuInici
Responsabilitat:
- Pantalla inicial.
- Botons:
  - Comencar (nova partida).
  - Continuar (nomes actiu si hi ha guardat).
  - Sortir.

Gestio:
- `MainMenuController` consulta `GameManager.TePartidaGuardada()` per activar/desactivar continuar.
- L'audio de menu es configura des del mateix controlador.

### 4.2 Passadis
Responsabilitat:
- Escena principal de joc.
- Inclou jugador, triggers de loop, anomalies i overlay de pausa.

Gestio:
- `GameManager` prepara l'escena en `OnSceneLoaded`.
- Connecta triggers, prepara ronda, teleporta a spawn, controla progressio i decisions.
- `CorridorLoopController` manté el bucle i la orientacio.
- `AnomalyManager` aplica o no anomalia a cada ronda.

### 4.3 GameOver
Responsabilitat:
- Pantalla final de victoria.
- Mostra missatge final, temps actual i record.
- Boto per tornar al menu.

Gestio:
- `GameOverController` llegeix dades publicades pel `GameManager`.

---

## 5. PLAYERPREFS I PERSISTENCIA

### 5.1 Dades que es guarden
Prefix general: `NoArribesTard_Save_`

Guardat de partida:
- `HasSave` (int)
- `PosX`, `PosY`, `PosZ` (float)
- `RotY` (float)
- `Pitch` (float)
- `Nivell` (int)
- `Temps` (float)
- `TeAnomalia` (int 0/1)
- `TipusAnomalia` (int)
- `IndexAnomalia` (int)

Record:
- `TempsRecord` (float)

### 5.2 Flux de guardat i carrega
Guardat:
- En pausa -> boto "Guardar i sortir".
- Tambe en `OnApplicationPause` i `OnApplicationQuit` si la partida esta en curs.

Carrega:
- En "Continuar partida" des del menu.
- `GameManager` aplica posicio, orientacio, nivell, temps i estat d'anomalia.

Esborrat:
- En "Comencar partida nova" s'esborra guardat de partida (no el record).
- En victoria s'esborra guardat de partida per forcar nova run.

---

## 6. PROBLEMES TROBATS I SOLUCIONS
- Salt visible de teleport:
  - Solucio: orientacio canonica i reposicionament amb marge fora trigger.
- Inversions de direccio en recorregut:
  - Solucio: teletransport sempre cap a inici canonical i rotacio controlada.
- Persistencia incompleta:
  - Solucio: snapshot complet (posicio + rotacio + nivell + temps + anomalia activa).
- Acumulacio d'efectes d'anomalies:
  - Solucio: restauracio global a cada canvi de ronda.
- Errors visuals de text/UI:
  - Solucio: separacio d'escenes i control de UI per scripts dedicats.

---

## 7. POSSIBLES MILLORES FUTURES
- Afegir mes anomalies subtils lligades a so i animacio.
- Ajustar dificultat dinamica segons percentatge d'encerts.
- Afegir taula de millors temps (top 5) en lloc d'un sol record.
- Integrar guardat al nuvol (PlayFab) per perfil d'usuari.
- Afegir opcions de configuracio d'audio i sensibilitat de camera.
- Afegir sistema de localitzacio multiidioma.

---

## 8. CONCLUSIONS FINALS
El projecte compleix els objectius principals:
- loop funcional en primera persona,
- sistema d'anomalies aleatori i restaurable,
- progressio clara per nivells,
- menus separats i funcionals,
- persistencia local de partida i record de temps.

El codi esta modularitzat per responsabilitats i facilita el manteniment i l'ampliacio futura.

---

## 9. WEBGRAFIA I RECURSOS UTILITZATS
- Unity Manual: https://docs.unity3d.com/Manual/index.html
- Unity Scripting API: https://docs.unity3d.com/ScriptReference/
- TextMeshPro: https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest
- Referencia conceptual de loop/anomalies (inspiracio): Exit 8

