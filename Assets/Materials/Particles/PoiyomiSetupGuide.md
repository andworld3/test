# Poiyomi AudioLink パーティクル設定ガイド

## 前提条件
- Poiyomi Toon 9.x がプロジェクトに導入済み
- AudioLink がシーンに配置済み
- パーティクルシステムが設定済み

## マテリアル作成手順

### 1. 基本マテリアル作成
1. Project ウィンドウで右クリック → Create → Material
2. マテリアル名を `AL_Particle_Poiyomi` に変更
3. Inspector で Shader を `Poiyomi/Poiyomi Toon` に変更

### 2. 基本設定
- **Base Color**: 基本となるパーティクル色を設定
- **Main Texture**: パーティクル用テクスチャを割り当て（任意）
- **Rendering Preset**: `Transparent` を選択

### 3. AudioLink モジュール設定

#### 3.1 AudioLink 有効化
- `AudioLink` セクションを展開
- `Enable AudioLink` にチェック

#### 3.2 Emission（発光）設定
- `Emission` セクションを展開
- `Enable Emission` にチェック
- `AudioLink` タブを選択
- 設定値：
  - **Band**: `Bass (0)`
  - **Mode**: `VU`
  - **Multiplier**: `2.0`
  - **Add**: `0.0`
  - **Smoothing**: `0.15`
  - **History**: `0.1`

#### 3.3 Color（色変化）設定
- `Main` セクションの `AudioLink` タブを選択
- 設定値：
  - **Band**: `LowMid (1)` または `HighMid (2)`
  - **Mode**: `Amplitude`
  - **Color Tint**: 反応時の色を設定
  - **Multiplier**: `1.5`
  - **Smoothing**: `0.2`

#### 3.4 Vertex Manipulation（頂点変位）設定（任意）
- `Vertex Options` → `AudioLink` を展開
- 設定値：
  - **Band**: `Bass (0)`
  - **Mode**: `Amplitude`
  - **Intensity**: `0.1`
  - **Smoothing**: `0.15`

### 4. パフォーマンス最適化設定

#### 4.1 アルファ設定
- `Alpha Options` セクション
- `Alpha Cutoff`: `0.1`（透明部分の除去）

#### 4.2 距離設定
- `Rendering Options` セクション
- `Distance Fade`: 必要に応じて設定（遠距離での非表示）

#### 4.3 LOD設定
- `Vertex Options` → `Vertex Colors`
- 距離に応じた品質調整を設定

### 5. 推奨AudioLink帯域マッピング

| エフェクト | 推奨帯域 | 理由 |
|------------|----------|------|
| Emission（発光） | Bass (0) | 低音域での力強い反応 |
| Color Tint（色調） | LowMid (1) / HighMid (2) | 中音域での滑らかな色変化 |
| Vertex Displacement | Bass (0) | 低音域での形状変化 |
| Alpha Modulation | High (3) | 高音域での細かい点滅 |

### 6. Smoothing パラメータ調整

#### 推奨値（プリセット別）
- **Calm**: Smoothing `0.25`, History `0.2`
- **Default**: Smoothing `0.15`, History `0.1`
- **Hype**: Smoothing `0.08`, History `0.05`

### 7. SafeMode対応設定

チラつき防止のため以下を調整：
- Emission Multiplier を `1.5` 以下に制限
- Smoothing を `0.2` 以上に設定
- History を `0.15` 以上に設定
- 急激な色変化を避けるため Color Add を使用

### 8. 検証手順

1. AudioLink Controller でテスト音声を再生
2. VU メーターが動作することを確認
3. パーティクルが音に反応することを確認
4. 以下の点をチェック：
   - 眩しすぎないか
   - チラつきが激しくないか
   - 無音時にも適度な表示があるか
   - フレームレートに影響していないか

### 9. MaterialPropertyBlock (MPB) 対応プロパティ

外部スクリプト（ALPresetController）から制御可能なプロパティ：
- `_AudioLinkEmissionMultiplier`: 発光強度
- `_AudioLinkColorMultiplier`: 色変化強度
- `_AudioLinkVertexMultiplier`: 頂点変位強度
- `_AudioLinkSmoothingBass`: Bass帯域のスムージング
- `_AudioLinkSmoothingLowMid`: LowMid帯域のスムージング

### 10. トラブルシューティング

#### 反応しない場合
1. AudioLink が有効になっているか確認
2. `Enable AudioLink` がチェックされているか確認
3. Multiplier が 0 になっていないか確認
4. AudioLink Controller に音源が設定されているか確認

#### チラつきが激しい場合
1. Smoothing 値を増加（0.2〜0.3）
2. History 値を増加（0.15〜0.25）
3. Multiplier 値を減少
4. SafeMode設定を適用

#### パフォーマンスが悪い場合
1. Distance Fade を設定
2. LOD を適用
3. パーティクル数を削減
4. Alpha Cutoff を調整