# 音效 Cue 参考表

本文用于给 FishSlapper 的跳水扇鱼机制做音效调试参考。

## 结论

- 本机 `Content/Data/AudioChanges.xnb` 可以正常读取，但当前版本导出结果为空表。
- 这意味着游戏内大量原版 cue 并不通过 `AudioChanges` 数据表暴露，而是内建在游戏音频资源/代码调用里。
- 因此下面的参考表分成两类：
  - 当前 mod 已实际使用的 cue。
  - 从本机 `Stardew Valley.dll` 字符串扫描得到、与跳跃/水体最相关的候选名。

## 当前代码使用

| 用途 | Cue | 位置 | 说明 |
| --- | --- | --- | --- |
| 扇鱼命中 | `iwyxdxl.FishSlapper_SlapSound` | mod 自定义 | `assets/slap.wav` |
| 跳水起跳 | `dwop` | [ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L5) | 原版 `Farmer.jump()` 使用的跳跃声 |
| 跳水入水 | `waterSlosh` | [ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L6) | 当前默认入水声，力度比 `dropItemInWater` 更重 |
| 跳水出水 | `waterSlosh` | [ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L7) | 当前默认出水声，优先强调涉水感 |
| 扇鱼成功结算 | `jingle1` | [VanillaFishingBridge.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/Vanilla/VanillaFishingBridge.cs#L98) | 沿用原版钓鱼成功提示 |
| 扇鱼失败结算 | `fishEscape` | [VanillaFishingBridge.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/Vanilla/VanillaFishingBridge.cs#L109) | 沿用原版逃鱼提示 |

## 水体/跳跃候选

以下名称来自对本机 `Stardew Valley.dll` 的字符串扫描，适合作为下一轮调试候选。它们不等于“全部可播的 cue 表”，但足够覆盖本 mod 关心的跳跃/水花方向。

| 候选名 | 方向 | 备注 |
| --- | --- | --- |
| `dwop` | 强相关 | 已确认被原版 `Farmer.jump()` 直接使用 |
| `dropItemInWater` | 强相关 | 已确认被原版 `FishingRod.DoFunction(...)` 直接使用，声音偏轻 |
| `pullItemFromWater` | 强相关 | 已确认被原版 `FishingRod.doPullFishFromWater(...)` 直接使用，声音偏轻 |
| `quickSlosh` | 中相关 | 已在 `GameLocation.sinkDebris(...)` 中出现，适合后续尝试做辅助水声 |
| `waterSlosh` | 强相关 | 已在 `GameLocation.playTerrainSound(...)` 中出现，当前版本已接入 |
| `bubbles` | 强相关 | 更偏持续水泡感，适合入水后停留时辅助 |
| `fishEscape` | 强相关 | 已被当前失败结算使用 |
| `swimming` | 中相关 | 更偏持续状态，不建议直接当瞬时落水声 |
| `playSlosh` | 弱相关 | 更像方法名，不建议直接当 cue |
| `startSplash` | 弱相关 | 更像逻辑名，不建议直接当 cue |
| `CreateSplash` | 弱相关 | 更像逻辑名，不建议直接播放 |
| `sound_waterfall` | 中相关 | 环境循环声，不适合瞬时入水 |
| `sound_waterfall_big` | 中相关 | 环境循环声，不适合瞬时入水 |

## 动作参考

当前跳水分身使用的都是 `FarmerSprite` 现成帧：

| 阶段 | 帧组 | 说明 |
| --- | --- | --- |
| 蓄力 / 入水 / 回岸 | `run` | 按原版抛竿方向选择四向跑动帧，优先保证朝向稳定 |
| 扇击瞬间 | `punch` | 复用原先 slap punch |
| 水中待机 | `walk` 站立帧 | 避免高动作帧长期残留 |

## 调试建议

- 想试别的起跳/入水/出水声，先改 [ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L5)、[ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L6)、[ModConstants.cs](/C:/Users/3DBox/Documents/CodeForFun/StardewValley_FishSlapper_Mod/FishSlapper/ModConstants.cs#L7)。
- 若当前 `waterSlosh` 还不够理想，下一轮优先试 `quickSlosh + dropItemInWater` 或 `quickSlosh + pullItemFromWater` 的双声叠加。
- 若后续继续排查原版 cue，优先看本文件列出的候选名，再去对照 `Stardew Valley.dll` 里相关方法的字符串和调用链。
