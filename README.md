# reflaming_hdrp_vj — Generative VJ "Ariake"

Unity (HDRP) で作った VJ。BSP（空間の分割）で並べたビル群の、サイズと高さを音と MIDI コントローラーで動的に変える。
A VJ set built in Unity HDRP: building clusters laid out by BSP change their size and height with the audio and a MIDI controller.

- 演奏：draw(); のライブイベント（2025年11月）— https://function-draw.com
- 映像：https://www.youtube.com/watch?v=R8c0UuTo-rM
- 主なスクリプト：`Assets/Scripts/`（SimpleBSPGenerator・ProceduralBuildingHybrid・AudioReactive・MidiController など）

## 開くとき
Unity 6（HDRP 17.0.4）。MIDI は [Minis](https://github.com/keijiro/Minis)。
Asset Store の素材（KT_SmartMat_Vol_6・SkySeries Freebie・SpaceZeta_StreetLamps2）は再配布できないので入れていない。開くときは各自で取り込む。
