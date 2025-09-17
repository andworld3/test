# ALUIController Udon Graph 設定ガイド

## 概要
ALUIController は、非エンジニア向けのワールド内パネルとして、AudioLink パーティクルシステムの設定を簡単に変更できるUdon Graphです。

## 必要なUI要素

### 1. Canvas設定
- Canvas Render Mode: `World Space`
- Canvas Scaler: UI Scale Mode を `Scale With Screen Size` に設定
- サイズ: 幅 400px、高さ 600px

### 2. UI コンポーネント構成

#### 2.1 タイトル
- **GameObject名**: `Title`
- **Component**: Text (Legacy) または TextMeshPro
- **Text**: "AudioLink Particle Control"
- **位置**: 上部中央

#### 2.2 プリセット選択
- **GameObject名**: `PresetDropdown`
- **Component**: Dropdown (Legacy)
- **Options**:
  - "Calm"
  - "Default"
  - "Hype"
- **Default Value**: 1 (Default)

#### 2.3 Master Gainスライダー
- **GameObject名**: `MasterGainSlider`
- **Component**: Slider
- **Min Value**: 0.0
- **Max Value**: 2.0
- **Value**: 1.0
- **Whole Numbers**: false

#### 2.4 Master Gainラベル
- **GameObject名**: `MasterGainLabel`
- **Component**: Text
- **Text**: "Master Gain: 1.00"

#### 2.5 Emission Gainスライダー
- **GameObject名**: `EmissionGainSlider`
- **Component**: Slider
- **Min Value**: 0.0
- **Max Value**: 3.0
- **Value**: 1.0
- **Whole Numbers**: false

#### 2.6 Emission Gainラベル
- **GameObject名**: `EmissionGainLabel`
- **Component**: Text
- **Text**: "Emission Gain: 1.00"

#### 2.7 Smoothingスライダー
- **GameObject名**: `SmoothingSlider`
- **Component**: Slider
- **Min Value**: 0.05
- **Max Value**: 0.5
- **Value**: 0.15
- **Whole Numbers**: false

#### 2.8 Smoothingラベル
- **GameObject名**: `SmoothingLabel`
- **Component**: Text
- **Text**: "Smoothing: 0.15"

#### 2.9 Safe Modeトグル
- **GameObject名**: `SafeModeToggle`
- **Component**: Toggle
- **Is On**: false

#### 2.10 Safe Modeラベル
- **GameObject名**: `SafeModeLabel`
- **Component**: Text
- **Text**: "Safe Mode"

#### 2.11 Beat Pulseトグル
- **GameObject名**: `BeatPulseToggle`
- **Component**: Toggle
- **Is On**: true

#### 2.12 Beat Pulseラベル
- **GameObject名**: `BeatPulseLabel`
- **Component**: Text
- **Text**: "Beat Pulse"

## Udon Graph ノード構成

### 1. 変数定義
以下の変数をUdon Graphで定義してください：

#### Public Variables（Inspector で設定）
```
ALPresetController presetController (GameObject)
Dropdown presetDropdown (UnityEngine.UI.Dropdown)
Slider masterGainSlider (UnityEngine.UI.Slider)
Slider emissionGainSlider (UnityEngine.UI.Slider)
Slider smoothingSlider (UnityEngine.UI.Slider)
Toggle safeModeToggle (UnityEngine.UI.Toggle)
Toggle beatPulseToggle (UnityEngine.UI.Toggle)
Text masterGainLabel (UnityEngine.UI.Text)
Text emissionGainLabel (UnityEngine.UI.Text)
Text smoothingLabel (UnityEngine.UI.Text)
```

#### Private Variables
```
bool isInitialized (Boolean)
float updateDelay (Single) = 0.1
```

### 2. Start イベント
```
Event: Start
1. Set isInitialized = false
2. Call "InitializeUI"
3. Set isInitialized = true
```

### 3. InitializeUI カスタムイベント
```
1. presetDropdown.value = presetController.currentPresetIndex
2. masterGainSlider.value = presetController.masterGain
3. emissionGainSlider.value = presetController.emissionGain
4. smoothingSlider.value = presetController.smoothing
5. safeModeToggle.isOn = presetController.safeMode
6. beatPulseToggle.isOn = presetController.beatPulse
7. Call "UpdateAllLabels"
```

### 4. UpdateAllLabels カスタムイベント
```
1. masterGainLabel.text = String.Concat("Master Gain: ", masterGainSlider.value.ToString("F2"))
2. emissionGainLabel.text = String.Concat("Emission Gain: ", emissionGainSlider.value.ToString("F2"))
3. smoothingLabel.text = String.Concat("Smoothing: ", smoothingSlider.value.ToString("F2"))
```

### 5. UI イベント処理

#### Dropdown変更時
```
Event: presetDropdown OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetPreset(presetDropdown.value)
3. SendCustomEventDelayedSeconds("UpdateFromPreset", updateDelay)
```

#### Master Gainスライダー変更時
```
Event: masterGainSlider OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetMasterGain(masterGainSlider.value)
3. masterGainLabel.text = String.Concat("Master Gain: ", masterGainSlider.value.ToString("F2"))
```

#### Emission Gainスライダー変更時
```
Event: emissionGainSlider OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetEmissionGain(emissionGainSlider.value)
3. emissionGainLabel.text = String.Concat("Emission Gain: ", emissionGainSlider.value.ToString("F2"))
```

#### Smoothingスライダー変更時
```
Event: smoothingSlider OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetSmoothing(smoothingSlider.value)
3. smoothingLabel.text = String.Concat("Smoothing: ", smoothingSlider.value.ToString("F2"))
```

#### Safe Modeトグル変更時
```
Event: safeModeToggle OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetSafeMode(safeModeToggle.isOn)
```

#### Beat Pulseトグル変更時
```
Event: beatPulseToggle OnValueChanged
1. If isInitialized == false, Return
2. presetController.SetBeatPulse(beatPulseToggle.isOn)
```

### 6. UpdateFromPreset カスタムイベント
```
1. masterGainSlider.value = presetController.masterGain
2. emissionGainSlider.value = presetController.emissionGain
3. smoothingSlider.value = presetController.smoothing
4. safeModeToggle.isOn = presetController.safeMode
5. beatPulseToggle.isOn = presetController.beatPulse
6. Call "UpdateAllLabels"
```

## 設定手順

### 1. UIオブジェクト作成
1. Hierarchy で右クリック → UI → Canvas
2. Canvas の子として上記UI要素を配置
3. Layout Group（Vertical Layout Group）を使用して整列

### 2. Udon Behaviour追加
1. Canvas に `UdonBehaviour` コンポーネントを追加
2. Program Source に新しい Udon Graph を作成・割り当て
3. 上記ノード構成に従ってグラフを作成

### 3. 参照設定
1. Inspector で Public Variables を設定
2. presetController に ALPresetController を持つGameObjectを割り当て
3. 各UI要素を対応する変数に割り当て

### 4. イベント接続
1. 各UI要素のイベント（OnValueChanged等）をUdon Behaviourの対応メソッドに接続
2. Dropdown, Slider, Toggle それぞれのイベントを設定

## トラブルシューティング

### UI が反応しない
1. isInitialized フラグが正しく設定されているか確認
2. presetController への参照が正しく設定されているか確認
3. UI要素のInteractableがtrueになっているか確認

### 値が同期されない
1. presetController の Networking 設定を確認
2. UpdateFromPreset イベントが正しく呼ばれているか確認

### パフォーマンス問題
1. updateDelay を適切に設定（0.1秒推奨）
2. 不要な毎フレーム更新を避ける
3. ToString の使用を最小限に抑える