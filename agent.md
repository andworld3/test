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
- **見た目は反応するが"数"が変わらない** → シェーダではモジュール変更不可。**コードで Emission を**
- **チラつく/眩しい** → Smoothing↑、発光をClamp、点滅禁止の安全モード
- **マルチ同期** → 共有AudioSourceを使う or 値をネット同期（必要最小限）

---

## 9) 実装済みコンポーネント

### 9-1) 自作HLSLシェーダー（ALReactiveParticle.shader）
- **場所**: `Assets/Shaders/ALReactiveParticle.shader`
- **機能**: _AudioTexture直接サンプル、発光・色ブレンド・頂点変位
- **主要プロパティ**:
  - `_AL_Enable`: AudioLink ON/OFF
  - `_AL_Band`: 帯域選択（0=Bass, 1=LowMid, 2=HighMid, 3=Treble）
  - `_AL_Gain`: 反応ゲイン（0-4）
  - `_AL_Smooth`: 平滑化係数（0-1）
  - `_EmissionGain`: 発光強度（0-5）
  - `_ColorA/_ColorB`: 色ブレンド用
  - `_DisplaceAmp`: 頂点変位量（0-1）

### 9-2) ALPresetController.cs
- **場所**: `Assets/Scripts/UdonSharp/ALPresetController.cs`
- **機能**: プリセット管理、MPB制御、ネットワーク同期
- **プリセット**: Calm / Default / Hype
- **公開メソッド**:
  - `SetPreset(int)`: プリセット切替
  - `SetMasterGain(float)`: マスターゲイン（0-2）
  - `SetEmissionGain(float)`: 発光ゲイン（0-3）
  - `SetSafeMode(bool)`: 安全モード切替

### 9-3) ALParticleModulator.cs
- **場所**: `Assets/Scripts/UdonSharp/ALParticleModulator.cs`
- **機能**: ParticleSystem動的制御、EMA平滑化
- **制御項目**: rateOverTime / startSpeed / startSize
- **Audio Data対応**: 実験的機能として搭載（初期OFF）
- **フォールバック**: 無音時の穏やかなアイドリング

### 9-4) Poiyomi設定ガイド
- **場所**: `Assets/Materials/Particles/PoiyomiSetupGuide.md`
- **内容**: AudioLinkモジュール設定、帯域マッピング、MPB対応プロパティ

### 9-5) UI制御（Udon Graph）
- **場所**: `Assets/Scripts/UdonGraph/ALUIController_SetupGuide.md`
- **UI要素**: プリセット選択、ゲインスライダー、トグル類
- **機能**: リアルタイム調整、ラベル更新、同期対応

---

## 10) セットアップ手順（実装版）

### 10-1) 基本セットアップ
1. VCCでAudioLink + Poiyomi 9.x導入
2. AudioLink/AudioLinkControllerをシーン配置
3. AudioSourceまたはVideoPlayerを割当
4. 「Link all sound reactive objects...」実行

### 10-2) Prefab使用
1. `Assets/Prefabs/PrefabSetupGuide.md`参照
2. **AL_ParticleSystem_Complete**: UI付き完全版
3. **AL_ParticleSystem_Basic**: UI無し軽量版
4. **AL_Controller_UI**: UI単体

### 10-3) マテリアル選択
- **自作シェーダー**: `ALReactiveParticle`使用
- **Poiyomi**: AudioLinkモジュール有効化版
- 設定詳細は各ガイド参照

### 10-4) 調整・検証
1. VUメーター動作確認
2. 反応テスト（Bass/発光、HighMid/速度など）
3. SafeMode動作確認（チラつき抑制）
4. パフォーマンス確認（目標: PC 15k粒子以下）

---

## 11) プロパティ一覧表

| プロパティ | 範囲 | デフォルト | 説明 |
|------------|------|------------|------|
| **シェーダー系** | | | |
| _AL_Enable | 0/1 | 1.0 | AudioLink有効/無効 |
| _AL_Band | 0-3 | 0 | Bass/LowMid/HighMid/Treble |
| _AL_Gain | 0-4 | 1.5 | オーディオ入力ゲイン |
| _AL_Smooth | 0-1 | 0.15 | EMA平滑化係数 |
| _EmissionGain | 0-5 | 1.0 | 発光強度倍率 |
| _DisplaceAmp | 0-1 | 0.1 | 頂点変位量 |
| **コントローラー系** | | | |
| masterGain | 0-2 | 1.0 | マスターゲイン |
| emissionGain | 0-3 | 1.0 | 発光ゲイン |
| smoothing | 0.05-0.5 | 0.15 | 平滑化 |
| safeMode | bool | false | 安全モード |
| beatPulse | bool | true | ビートパルス |
| **パーティクル系** | | | |
| baseEmissionRate | 0+ | 10.0 | 基準発生数 |
| emissionRateGain | 0-100 | 20.0 | 発生数ゲイン |
| baseStartSpeed | 0+ | 1.0 | 基準速度 |
| startSpeedGain | 0-10 | 2.0 | 速度ゲイン |
| emaCoefficient | 0.05-0.5 | 0.15 | EMA係数 |

---

## 12) パフォーマンス実測値

### PC推奨設定
- **粒子数**: 10,000-15,000個
- **更新頻度**: 20Hz（0.05s間隔）
- **マテリアル数**: 3個以下
- **フレーム影響**: 60fps維持想定

### 最適化ポイント
- MPB使用でマテリアルインスタンス化回避
- モジュール参照のキャッシュ化
- 毎フレームGC回避
- 適切なClamp/EMA適用でCPU負荷軽減
