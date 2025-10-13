# Epic-Legions

<p align="center">
  <a href="Epic%20Legions/Assets/Sprites/UI/Logo%202.png">
    <img src="Epic%20Legions/Assets/Sprites/UI/Logo%202.png" alt="Logo" width="320">
  </a>
</p>

Documentación generada por IA: https://deepwiki.com/MarkLuffyGames/Epic-Legions

# 🎮 Game Design Document (GDD) – *Hemera Legion*

---

## 1. 📌 Información General

- 🎴 **Título del juego:** Hemera Legion
- 🧩 **Género:** TCG (Trading Card Game) – Juego de cartas coleccionables competitivo
- 💻📱 **Plataformas:** PC y Móvil (Android / iOS)
- 👥 **Público objetivo:** Jugadores casuales y competitivos (15–40 años)
- 📖 **Estilo narrativo:** Fantasía oscura con toques de humor
- 💰 **Modelo de negocio:** Free-to-play con sobres de cartas y cosméticos

---

## 2. 🌌 Concepto General

*Hemera Legion* es un **TCG de duelos entre dos jugadores**, donde los héroes de la antigua **Legión Hemera** fueron fragmentados en cartas.

Cada partida es una **batalla estratégica** en el continente de **Etheryon**, usando héroes, hechizos y equipamientos.

---

## 3. 🗺️ Mundo y Lore

- 🌍 **Mundo:** Etheryon
- ⚔️ **Contexto:** Guerra entre facciones por el poder de los héroes fragmentados
- 📜 **Narrativa:**
    - Los héroes fueron luchadores del pasado cuya energía se dispersó.
    - Cada carta es un fragmento de ese héroe.
    - El mismo héroe puede aparecer varias veces en un duelo.
    - “Hemera” es el nombre de la antigua legión de héroes.

---

## 4. 🃏 Tipos de Cartas

1. 👑 **Héroes**
2. ✨ **Hechizos**
3. 🛡️ **Equipamientos**

---

## 5. 💎 Rarezas

- ⚪ **Común**
- 🔵 **Rara**
- 🟣 **Épica**
- 🟠 **Legendaria**

---

## 6. 📊 Estadísticas de los Héroes

- Escala: **0 a 100**
- Atributos principales:
    - ❤️ Vida (HP)
    - 🛡️ Defensa (DEF)
    - ⚡ Velocidad (SPD)
    - 🏹 Clase
    - 🌌 Elemento

---

## 7. 🧙‍♂️ Clases de Héroes

- ⚔️ **Warrior** – Versátil
- ✝️ **Paladin** – Guerrero sagrado
- 🔮 **Wizard** – Magia elemental
- 🏹 **Hunter** – Arcos, trampas
- 🗡️ **Assassin** – Emboscadas
- 🌿 **Druid** – Naturaleza
- 🐺 **Beast** – Criaturas salvajes
- 🗿 **Colossus** – Gigantes destructivos
- ☠️ **Necromancer** – No-muertos

---

## 8. 🌌 Elementos

- 🔥 **Fuego**
- 💧 **Agua**
- 🌿 **Planta**
- ⛰️ **Tierra**
- ⚡ **Rayo**
- 🌪️ **Viento**
- ✨ **Luz**
- 🌑 **Oscuridad**

➡️ Sistema de fortalezas y debilidades cíclicas (ejemplo: Fuego > Planta, Planta > Agua, etc.)

---

## 9. ⚙️ Sistema de Equipamiento

- Cada héroe puede portar:
    - ⚔️ **1 arma** → añade ataques extra
    - 🛡️ **1 armadura** → aumenta DEF o cambia elementos
    - 💍 **1 accesorio** → habilidades pasivas
- Escudos como variante de arma

---

## 10. 🎲 Mecánicas del Juego

- 🔋 **Energía:**
    - Máx. 100, inicio 100, +20 por turno
- 🏟️ **Campo de batalla:**
    - 15 posiciones (3 filas × 5 columnas)
    - Reglas de ataque:
        - Primera línea protege a las de atrás
        - Hunters pueden atacar 2ª línea
        - Assassins pueden atacar 3ª línea
- 🛡️ **Defensa:**
    - Se regenera al máximo al final de cada turno
    - Puede ser alterada por buffs/debuffs
- 🎯 **Tipos de ataque:**
    - Un objetivo
    - Objetivo + detrás
    - Columna completa
    - Objetivo + adyacentes
    - Abanico (objetivo + detrás + adyacentes)
    - Alrededor
    - Todo el campo

---

## 11. ⏳ Flujo de un Duelo

1. 🏁 **Inicio**
    - Mazo de 40 cartas (sin repetidas)
    - Mano inicial: 7
2. 🔄 **Turnos simultáneos**
    - Planificación → resolución por velocidad
3. 🗡️ **Fases del turno**
    - Inicio: robar carta + energía
    - Preparación: invocar/equipar/lanzar hechizos
    - Batalla (mini-turnos):
        1. Buffs y curaciones
        2. Daños y debuffs
        3. Efectos secundarios
        4. Eliminación de héroes derrotados
4. 🏆 **Victoria**
    - Vida del rival = 0
    - Condiciones especiales (modo historia/eventos)

---

## 12. 💰 Economía

- 🎴 **Sobres de cartas** → recompensas aleatorias
- 🏆 **Recompensas** → historia, competitivo y eventos

---

## 13. 🎨 Arte y Estética

- Estilo: 🖤 Oscuro con ✨ detalles dorados y geometría celestial
- Reverso: Logo *Hemera Legion* en fondo estelar 🌌
- Frente: Marcos elegantes, iconos de estadísticas/clase, ilustraciones detalladas

---

## 14. 🖥️ Implementación Técnica

- ⚙️ Motor: Unity
- 📦 Cartas: ScriptableObjects
- 🌐 Multijugador: Lobby + Relay (casual)
- 🖥️ Pendiente: Servidor dedicado competitivo
- 🤖 IA: estrategias dinámicas
- 🖌️ UI/UX: iconos claros, indicadores de elementos, animaciones con shaders y partículas

---

## 15. 🚀 Roadmap Futuro

- 📖 Modo historia con facciones
- 🖥️ Servidor dedicado competitivo
- 🌌 Hechizos de campo en futuras expansiones
- 🎉 Eventos especiales con condiciones alternativas


![Game Screenshot](Epic%20Legions/Assets/Sprites/UI/LoadScreen.png)


<p>
  <a href="Epic%20Legions/Assets/Sprites/Cards/Heroes/lord%20final.png">
    <img src="Epic%20Legions/Assets/Sprites/Cards/Heroes/lord%20final.png" alt="Lord" width="240">
  </a>
  &nbsp;&nbsp;
  <a href="Epic%20Legions/Assets/Sprites/Cards/Spells/Poción%20de%20vitalidad.png">
    <img src="Epic%20Legions/Assets/Sprites/Cards/Spells/Poción%20de%20vitalidad.png" alt="Poción de vitalidad" width="240">
  </a>
  &nbsp;&nbsp;
  <a href="Epic%20Legions/Assets/Sprites/Cards/Reverse%20Card%20v3.1.png">
    <img src="Epic%20Legions/Assets/Sprites/Cards/Reverse%20Card%20v3.1.png" alt="Reverse Card" width="240">
  </a>
</p>

