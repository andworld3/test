# AudioLink Particle System Prefab 構成ガイド

## 作成するPrefab一覧

### 1. AL_ParticleSystem_Complete
完全な AudioLink 反応パーティクルシステム

### 2. AL_Controller_UI
UI コントロールパネル

### 3. AL_ParticleSystem_Basic
基本的なパーティクルシステム（UI なし）

## 1. AL_ParticleSystem_Complete Prefab

### 構成
```
AL_ParticleSystem_Complete (Empty GameObject)
├── ParticleSystem (Particle System)
├── ALPresetController (GameObject with ALPresetController.cs)
├── ALParticleModulator (GameObject with ALParticleModulator.cs)
└── UI_Panel (Canvas)
    └── ALUIController (UdonBehaviour with UI elements)
```

### 設定手順

#### 1.1 ルートオブジェクト作成
1. Hierarchy で空のGameObjectを作成
2. 名前を `AL_ParticleSystem_Complete` に変更

#### 1.2 ParticleSystem設定
1. ルートの子として Particle System を追加
2. Particle System の設定：
   - **Duration**: 5.0
   - **Looping**: true
   - **Start Lifetime**: 3.0
   - **Start Speed**: 1.0
   - **Start Size**: 1.0
   - **Start Color**: White
   - **Max Particles**: 1000

3. **Emission** モジュール：
   - **Rate over Time**: 10 (基準値、スクリプトで動的変更)

4. **Shape** モジュール：
   - **Shape**: Sphere
   - **Radius**: 0.5

5. **Velocity over Lifetime** モジュール：
   - **Linear**: (0, 2, 0)

6. **Color over Lifetime** モジュール：
   - **Color**: グラデーション設定

7. **Size over Lifetime** モジュール：
   - **Size**: カーブ設定（生成時大→終了時小）

#### 1.3 ALPresetController設定
1. 空のGameObjectを作成、名前を `ALPresetController` に
2. `ALPresetController.cs` をアタッチ
3. Target Renderers に ParticleSystem の Renderer を割り当て

#### 1.4 ALParticleModulator設定
1. 空のGameObjectを作成、名前を `ALParticleModulator` に
2. `ALParticleModulator.cs` をアタッチ
3. Particle Systems に ParticleSystem を割り当て
4. 設定値：
   - **Base Emission Rate**: 10.0
   - **Emission Rate Gain**: 20.0
   - **Base Start Speed**: 1.0
   - **Start Speed Gain**: 2.0
   - **EMA Coefficient**: 0.15

#### 1.5 UI Panel設定
1. Canvas を作成（World Space）
2. `ALUIController_SetupGuide.md` に従ってUI要素を作成
3. UdonBehaviour をアタッチし、Udon Graph を設定

#### 1.6 マテリアル設定
1. ParticleSystem の Renderer → Materials に以下のいずれかを設定：
   - 自作シェーダー（`ALReactiveParticle`）を使用したマテリアル
   - Poiyomi設定済みマテリアル

## 2. AL_Controller_UI Prefab

### 構成
```
AL_Controller_UI (Canvas)
├── Background (Image)
├── Title (Text)
├── PresetSection (GameObject)
│   ├── PresetLabel (Text)
│   └── PresetDropdown (Dropdown)
├── SlidersSection (GameObject)
│   ├── MasterGainSlider (Slider with Label)
│   ├── EmissionGainSlider (Slider with Label)
│   └── SmoothingSlider (Slider with Label)
└── TogglesSection (GameObject)
    ├── SafeModeToggle (Toggle with Label)
    └── BeatPulseToggle (Toggle with Label)
```

### 設定手順
1. `ALUIController_SetupGuide.md` に従って作成
2. 独立したPrefabとして保存
3. 必要に応じて既存システムに追加可能

## 3. AL_ParticleSystem_Basic Prefab

### 構成
```
AL_ParticleSystem_Basic (Empty GameObject)
├── ParticleSystem (Particle System)
├── ALPresetController (GameObject with ALPresetController.cs)
└── ALParticleModulator (GameObject with ALParticleModulator.cs)
```

### 設定手順
1. `AL_ParticleSystem_Complete` からUI Panel を除いたバージョン
2. プログラムでの制御専用
3. より軽量で高パフォーマンス

## Prefab使用手順

### 1. シーンへの配置
1. AudioLink と AudioLinkController がシーンに配置済みであることを確認
2. Prefab をシーンにドラッグ&ドロップ
3. 位置・回転・スケールを調整

### 2. AudioLink接続
1. AudioLinkController で「Link all sound reactive objects to this AudioLink」を実行
2. _AudioTexture が正しく設定されていることを確認

### 3. 初期設定確認
1. ALPresetController の Target Renderers が正しく設定されているか確認
2. ALParticleModulator の Particle Systems が正しく設定されているか確認
3. Audio Source の割り当て（必要に応じて）

### 4. テスト
1. 音楽を再生してAudioLink VUメーターが動作することを確認
2. パーティクルが音に反応することを確認
3. UI操作（CompleteまたはUIプレハブ使用時）が正しく動作することを確認

## カスタマイズポイント

### 1. パーティクル外観
- **Texture**: パーティクルテクスチャの変更
- **Color**: 基本色・発光色の調整
- **Size**: サイズ分布の調整
- **Lifetime**: パーティクル寿命の調整

### 2. 音響反応
- **Band Selection**: 反応する周波数帯域の変更
- **Gain**: 反応感度の調整
- **Smoothing**: 反応の滑らかさ調整

### 3. パフォーマンス
- **Max Particles**: 最大パーティクル数の制限
- **Update Interval**: 更新頻度の調整
- **Distance Culling**: 距離による描画制限

## トラブルシューティング

### パーティクルが表示されない
1. ParticleSystem の Play On Awake が有効か確認
2. マテリアルが正しく設定されているか確認
3. Camera の Culling Mask 設定を確認

### 音に反応しない
1. AudioLink の VU メーターが動作しているか確認
2. _AudioTexture がグローバルに設定されているか確認
3. ALPresetController の AL_Enable が 1.0 に設定されているか確認

### UI が操作できない
1. Canvas の Render Mode が正しく設定されているか確認
2. UI要素の Raycast Target が有効か確認
3. Udon Graph の参照が正しく設定されているか確認

### パフォーマンスが悪い
1. パーティクル数を削減
2. 更新頻度を下げる（Update Interval を増加）
3. 距離による描画制限を設定
4. シェーダーの複雑さを軽減

## 最適化ガイドライン

### PC用設定
- Max Particles: 10,000〜15,000
- Update Interval: 0.05〜0.1秒
- 高品質シェーダー使用可能

### Quest用設定（将来対応）
- Max Particles: 2,000以下
- Update Interval: 0.1〜0.2秒
- シンプルなシェーダーを使用
- Soft Particles 無効
- テクスチャ圧縮必須