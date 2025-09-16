# agent.md — AudioLink × Particle System（Shader＋Code 制御）

## 0) GOAL
- パーティクルを **音に同期** させる。  
- **見た目の反応＝Shader**、**挙動の変化（発生数/速度等）＝コード**。

---

## 1) DECISION FLOW（最初に決める）
- 対象：**VRChat（Built-in RP）**
- 粒子：**Shuriken**
- 反応の主軸：**PoiyomiのAudio Link** or **自作HLSL**  
- Quest対応なら：粒子数/発光/ソフトパーティクルを厳しめ制限

---

## 2) SETUP（5分）
1. VCCで **AudioLink** を導入 → シーンに `AudioLink`/`AudioLinkController` を配置  
2. 音源（`AudioSource`or`VideoPlayer`）を用意して再生  
3. パーティクル用 `Material` に **AudioLink対応シェーダ**（Poiyomi等）を割当  
4. 反応確認：AudioLinkのVU/スペクトラムが動いていること  
5. 以降、**見た目＝Shader**／**挙動＝コード** で足していく

---

## 3) SHADER 制御（最短）
### 3-1) Poiyomi を使う場合
- Material > **Audio Link** をON  
- 各項目（Emission/Color/Vertex等）で **Band**（Bass/LowMid/HighMid/High）を割当  
- **Smoothing/History** を上げるとチラつき減

### 3-2) 自作HLSL（最低限の考え方）
- `_AudioTexture` をサンプル → 0–1 の帯域値を取得 → `emission/color/vertex offset` に乗算
- 擬似コード：
```hlsl
float band = SampleAudioBand(_AudioTexture, BAND_BASS); // 0..1
float3 col = lerp(_BaseColor.rgb, _BeatColor.rgb, saturate(band*gain));
o.Emission = col * _EmissionPower;
v.vertex.xyz += v.normal * (band * _DisplaceAmp);
```
- ノイズ対策：`bandSmooth = lerp(prev, band, 0.1);`／`saturate()`で上振れ抑制

---

## 4) CODE 制御（Shuriken を動的更新）
### 4-1) 何を触る？
- `Emission.rateOverTime` / `Main.startSpeed` / `Main.startSize` / `Noise.strength`
- 目安：**低音→発生数/サイズ**、**高音→速度/微細揺れ**

### 4-2) 平滑化＆安全
- `level = Lerp(prev, rawLevel, 0.15f);`
- `Mathf.Clamp` で上下限を固定（VRの快適性を最優先）

### 4-3) UdonSharp 風の最小パターン（概念）
```csharp
public ParticleSystem ps;
float level, prev;
void Update(){
  // 例：AudioLinkのBass値を取得して rawLevel に
  level = Mathf.Lerp(prev, rawLevel, 0.15f);
  var em = ps.emission; var main = ps.main;
  em.rateOverTime = baseRate + level * rateGain;      // 発生数
  main.startSpeed  = baseSpeed + level * speedGain;    // 速度
  prev = level;
}
```
※ 実装では AudioLink から所望の Band 値を取得する（なければ `AudioSource.GetOutputData` 等で代替）。

---

## 5) MAPPING（おすすめ初期割当）
- **Bass** → Emission（発生数）、Size、発光
- **LowMid/HighMid** → 色相ブレンド、頂点のゆらぎ
- **High** → 速度、スパーク発生
- **Beat** → 瞬間バースト or Rim 強調（短いパルス）

---

## 6) PERFORMANCE（ざっくり予算）
- PC：同時粒子 10k–20k 目安  
- Quest：数千以下／テクスチャ圧縮／発光控えめ／ソフトパーティクルOFF  
- コード側：モジュール参照の再取得禁止・GCゼロ（キャッシュ）

---

## 7) DEBUG CHECKLIST
- AudioLinkのVUが動いているか  
- Materialの **Audio Link 有効化** 忘れなし  
- 反応が弱い→Smoothing/History/帯域変更  
- ビルド後：音源の距離減衰/ミュート/レイヤーに注意

---

## 8) FALLBACKS（困った時）
- **見た目は反応するが“数”が変わらない** → シェーダではモジュール変更不可。**コードで Emission を**  
- **チラつく/眩しい** → Smoothing↑、発光をClamp、点滅禁止の安全モード  
- **マルチ同期** → 共有AudioSourceを使う or 値をネット同期（必要最小限）
