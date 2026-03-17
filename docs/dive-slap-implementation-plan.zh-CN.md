# 钓鱼时跳水扇鱼功能实现方案

## 1. 文档目的

本文档面向后续实现该功能的 agent 或开发者，目标是：

- 明确玩法目标与边界。
- 给出推荐的技术路线，而不是泛泛讨论。
- 降低一次性重写钓鱼系统的风险。
- 优先选择“可交付、可调试、可逐步增强”的实现方式。

本文档默认基于当前项目结构：

- 当前 mod 入口目前集中在 `FishSlapper/ModEntry.cs`。
- 当前 mod 已有输入处理、逐帧更新、世界绘制、Harmony 绘制补丁。
- 当前 mod 以娱乐性为主，不追求与原版钓鱼系统 100% 深度耦合。

但本次功能实现不应继续把主要逻辑堆在 `ModEntry.cs` 中。

## 2. 目标体验

当玩家进入钓鱼小游戏后：

1. 玩家按下一个专用动作键。
2. 角色执行一次“跳向鱼钩位置”的演出。
3. 角色看起来像是跳进了水里，只露出上半身。
4. 在限定时间内连续扇鱼达到指定次数。
5. 成功则直接把鱼钓上来。
6. 角色返回原来的岸上位置。
7. 失败则直接进入原版钓鱼失败流程。

## 3. 实现原则

### 3.1 不要真的把玩家逻辑上移动到水里

做“视觉跳水”，不做“真实水中移动”：

- 逻辑上仍停留在原位置附近，或只做受控的临时位移。
- 视觉上把农夫绘制到鱼钩附近。
- 用水花、水面遮罩、层级覆盖制造“半身入水”的观感。

这样做可以避免以下问题：

- 碰撞箱落在不可走水面上导致异常。
- 玩家状态、工具状态、菜单状态互相打架。
- 多人同步和位置回滚复杂化。
- 节日、剧情、过场、传送等状态下更容易出错。

### 3.2 优先复用原版结算，不要自造鱼结果

推荐不要自己生成“钓到了什么鱼”的结果，而是：

- 保留原版 `BobberBar` 里的鱼种、品质、宝箱结果。
- 跳水成功时，把原版钓鱼条推进到“即将成功”的状态。
- 让原版继续完成结算。

这样可以降低和以下内容的耦合：

- 稀有鱼判定。
- 完美钓鱼奖励。
- 宝箱捕获。
- 鱼塘、鱼饵、特殊规则。

### 3.3 先做假半身入水，再考虑真裁剪

第一版推荐：

- 正常绘制农夫。
- 在农夫腰部以下叠一层水面遮罩和水花效果。
- 让视觉看起来像“下半身在水下”。

不要一开始就做：

- `FarmerRenderer` 深度 Harmony 裁剪。
- 对农夫所有图层分别裁剪。
- 针对全部发型、帽子、袖子、武器层做精确兼容。

### 3.4 失败必须直接进入原版钓鱼失败流程

设计要求已经明确：

- 跳水扇鱼失败后，不恢复到普通 `BobberBar` 流程。
- 跳水扇鱼失败后，直接进入原版钓鱼失败结算。

这意味着实现上必须避免：

- 在失败后仅仅解除 `BobberBar.update` 冻结。
- 在失败后继续让玩家补救当前鱼。
- 在失败后回到“普通钓鱼条继续挣扎”的中间状态。

推荐做法：

- `DiveSlapSession` 标记失败后，主动触发原版失败路径。
- 如果没有公开方法可直接调用，则通过 Harmony 在 `BobberBar.update` 中注入一个“立即失败”的分支。
- 该失败分支应与原版失败结果一致，而不是自定义一个近似失败状态。

### 3.5 `ModEntry` 只负责接线，不负责业务细节

本次功能实现应明确做结构拆分：

- `ModEntry` 只保留 `Entry()`、事件订阅、配置读取、Harmony 注册。
- 玩法状态机、渲染、原版桥接、补丁逻辑应拆到独立文件。
- 不要把状态对象、渲染逻辑、输入判定、原版流程控制全部塞进 `ModEntry.cs`。

判定标准：

- `ModEntry` 应更像组合根，而不是功能实现类。
- 大部分私有字段和方法应搬到独立组件中。

## 4. 已确认可用的游戏接口

以下成员已确认在当前本机 Stardew Valley / SMAPI 环境中存在，可作为实现依据。

### 4.1 `FishingRod`

可用字段或方法包括：

- `bobber`
- `fishCaught`
- `whichFish`
- `fishSize`
- `fishQuality`
- `treasureCaught`
- `showingTreasure`
- `pullingOutOfWater`
- `doneFishing(Farmer who, bool consumeBaitAndTackle)`
- `pullFishFromWater(...)`

实现上最关键的是：

- `rod.bobber.Get()` 可拿到鱼钩的世界坐标。
- `rod.whichFish`、`rod.fishSize`、`rod.fishQuality` 等能辅助调试。
- 成功后优先让原版收尾，而不是直接自己生成鱼对象。

### 4.2 `BobberBar`

可用字段或方法包括：

- `distanceFromCatching`
- `perfect`
- `treasureCaught`
- `treasure`
- `whichFish`
- `fishSize`
- `fishQuality`
- `handledFishResult`
- `update(GameTime)`
- `draw(SpriteBatch)`

实现上最关键的是：

- `distanceFromCatching` 可作为成功推进点。
- `perfect` 和 `treasureCaught` 可以决定是否保留原版额外收益。
- `BobberBar` 本身可继续由原版绘制，只在特定阶段冻结或覆盖输入。

### 4.3 `Farmer`

可用字段或方法包括：

- `LerpPosition(...)`
- `synchronizedJump(float velocity)`
- `Halt()`
- `CanMove`
- `freezePause`
- `changeIntoSwimsuit()`
- `changeOutOfSwimSuit()`

实现上建议：

- 第一版优先用 `LerpPosition` 或自定义插值完成“跳向鱼钩”的演出。
- `synchronizedJump` 可用来补跳跃感。
- 不建议第一版启用泳装切换，除非视觉风格确实需要。

## 5. 推荐玩法定义

### 5.1 触发条件

只有同时满足以下条件时，才允许开始跳水扇鱼：

- `Context.IsWorldReady == true`
- 本地玩家当前工具是 `FishingRod`
- `Game1.activeClickableMenu is BobberBar`
- 当前没有已经激活的跳水会话
- 玩家不在剧情、节日、睡觉、传送等特殊状态

### 5.2 成功条件

推荐默认值：

- 限时：`90` tick（约 1.5 秒）
- 需要扇击次数：`5`

成功后行为：

- 结束跳水阶段。
- 让钓鱼结果进入原版成功结算。
- 播放音效、粒子和回位动画。

### 5.3 失败条件

推荐默认逻辑：

- 时间耗尽但扇击次数不足。
- 跳水阶段被异常中断。

失败后行为建议：

- 角色返回原位。
- 直接进入原版钓鱼失败流程。

说明：

- 这里的“原版钓鱼失败流程”指原版在鱼逃跑时的处理链路。
- 不是简单关闭跳水状态后继续原版 `BobberBar.update`。
- 不是恢复到跳水前的捕获进度。


## 6. 推荐实现架构

### 6.1 新增状态对象

推荐新增一个会话对象，例如 `DiveSlapSession`，用于管理一次跳水行为的完整生命周期。

建议字段：

```csharp
internal sealed class DiveSlapSession
{
    public FishingRod Rod = null!;
    public BobberBar BobberBar = null!;
    public DiveSlapState State;

    public Vector2 OriginalPlayerPosition;
    public Vector2 TargetBobberPosition;
    public Vector2 RenderPosition;

    public int RemainingTicks;
    public int RequiredHits;
    public int CurrentHits;

    public bool IsReturning;
    public bool IsResolvingSuccess;
    public bool IsCancelled;

    public int PreviousFacingDirection;
    public bool PreviousCanMove;
    public int PreviousFreezePause;
}
```

不要写在 `ModEntry.cs` 内部，拆出去。

### 6.2 推荐增加一个状态枚举

```csharp
internal enum DiveSlapState
{
    None,
    Windup,
    Diving,
    Slapping,
    ResolveSuccess,
    ResolveFail,
    Returning
}
```

推荐不要只靠多个 `bool` 拼状态，否则后面很容易互相打架。

### 6.3 文件建议

推荐至少拆成以下结构：

- `FishSlapper/ModEntry.cs`
  - 仅负责事件接线
  - 仅负责 Harmony patch 注册
  - 仅负责创建和持有核心组件实例
- `FishSlapper/ModConfig.cs`
  - 配置项定义
- `FishSlapper/Gameplay/DiveSlapController.cs`
  - 跳水扇鱼的主状态机
  - 输入响应后的业务入口
  - 成功/失败/取消的流程切换
- `FishSlapper/Gameplay/DiveSlapSession.cs`
  - 单次会话状态
- `FishSlapper/Gameplay/DiveSlapState.cs`
  - 状态枚举
- `FishSlapper/Rendering/DiveSlapRenderer.cs`
  - 跳水中的农夫绘制
  - 水面遮罩
  - 扇击提示与世界层视觉反馈
- `FishSlapper/Patches/BobberBarPatch.cs`
  - `BobberBar.update`
  - `BobberBar.receiveKeyPress`
  - `BobberBar.receiveLeftClick`
- `FishSlapper/Patches/Game1Patch.cs`
  - 当前已有的 `Game1.drawTool` 相关补丁
- `FishSlapper/Vanilla/VanillaFishingBridge.cs`
  - 与 `FishingRod` / `BobberBar` 的桥接
  - 原版成功推进
  - 原版失败触发

### 6.4 各组件职责边界

推荐职责如下：

#### `ModEntry`

- 创建 `DiveSlapController`
- 把 SMAPI 事件转发给 controller
- 注册 Harmony patch

不应包含：

- 具体状态迁移逻辑
- 视觉绘制逻辑
- 原版钓鱼成功/失败控制细节

#### `DiveSlapController`

- 判断是否能开始跳水
- 创建和销毁 `DiveSlapSession`
- 推进状态机
- 调用 renderer 和 vanilla bridge

不应包含：

- Harmony patch 本体
- 大量底层绘制细节

#### `DiveSlapRenderer`

- 只关心如何画
- 不决定成功或失败
- 不直接改写 `BobberBar` 状态

#### `VanillaFishingBridge`

- 封装对 `FishingRod` / `BobberBar` 的直接操作
- 封装成功推进和失败触发
- 把“原版怎么被驱动”从 controller 中隔离出去

这样做的好处是：

- 以后调失败流程时，不需要在多个文件里找逻辑。
- 以后调视觉时，不会误伤游戏流程。
- `ModEntry` 可以保持很薄。

## 7. 事件与补丁切入点

### 7.1 `Input.ButtonPressed`

用途：

- 在 `BobberBar` 激活时监听“开始跳水”按键。
- 在跳水阶段监听“扇击”按键。
- 使用 `Helper.Input.Suppress(...)` 屏蔽本次按键继续传给原版逻辑。

建议：

- 跳水开始键和扇击键可复用同一个配置键。
- 第一版不要复用左键，避免和原版钓鱼条输入冲突。

### 7.2 `GameLoop.UpdateTicked`

用途：

- 推进跳水会话状态机。
- 处理插值移动、计时器、粒子、回位。
- 判断成功、失败、取消。

### 7.3 `Display.RenderingWorld` / `Display.RenderedWorld`

用途：

- 继续复用现有粒子特效逻辑。
- 在世界层绘制“跳水中的农夫影像”。
- 绘制水面遮罩、水花、命中反馈。

### 7.4 `Display.RenderedActiveMenu`

用途：

- 在 `BobberBar` 上层绘制提示文本。
- 显示剩余时间、已扇次数、目标次数。
- 视觉上告诉玩家当前处于特殊机制。

### 7.5 `Display.MenuChanged`

用途：

- 检测 `BobberBar` 是否关闭。
- 如果菜单意外关闭，取消当前跳水会话并清理状态。

### 7.6 可选 Harmony patch

推荐保留当前已有的：

- `Game1.drawTool(Farmer, int)` 前置补丁

新增补丁建议按优先级选择：

1. `BobberBar.update` prefix
2. `BobberBar.receiveLeftClick` prefix（可选）
3. `BobberBar.receiveKeyPress` prefix（可选）

#### `BobberBar.update` prefix 的用途

当跳水阶段激活时：

- 允许冻结原版钓鱼条进度。
- 避免玩家一边跳水一边还在丢失进度。
- 让自定义小游戏在短时间内独占节奏。
- 在失败状态下，为原版失败分支注入统一入口。

推荐实现思路：

- 正常跳水阶段：prefix 返回 `false`，冻结 `BobberBar.update`。
- 成功状态：解除冻结，并把 `distanceFromCatching` 推到成功阈值。
- 失败状态：不要简单解除冻结；而是让 patch 在下一帧直接走原版失败分支。

#### 为什么不建议一开始就 patch `FarmerRenderer.draw`

因为这会显著提高渲染复杂度：

- 农夫由多个分层贴图组成。
- 帽子、头发、手臂、袖子、工具层可能产生兼容问题。
- 当前 mod 的目标是娱乐性，不值得第一版就深挖这一层。

## 8. 推荐状态机

### 8.1 `None`

无会话。

进入条件：

- 默认状态。

退出条件：

- 在 `BobberBar` 期间按下跳水键，创建新会话。

### 8.2 `Windup`

开始跳水前摇。

进入时：

- 记录原始位置。
- 读取鱼钩世界坐标。
- 锁定玩家移动。
- 设置面向方向。
- 触发跳跃和音效。

持续时间建议：

- `8` 到 `12` tick。

### 8.3 `Diving`

从岸边向鱼钩位置移动。

进入时：

- 启动 `RenderPosition` 插值。
- 可选地让真实玩家位置小幅跟随，或完全只做渲染位置变化。

退出条件：

- 到达目标位置附近。

### 8.4 `Slapping`

主要互动阶段。

进入时：

- 开始倒计时。
- 冻结 `BobberBar.update`。
- 允许按键累计扇击次数。

每次扇击时：

- 播放 slap 音效。
- 触发 punch 帧或 slap 粒子。
- 增加 `CurrentHits`。

成功条件：

- `CurrentHits >= RequiredHits`

失败条件：

- `RemainingTicks <= 0`

### 8.5 `ResolveSuccess`

成功结算阶段。

推荐行为：

- 停止跳水输入。
- 推进原版鱼条到成功状态。
- 播放更强的命中特效。

### 8.6 `ResolveFail`

失败结算阶段。

推荐行为：

- 停止跳水输入。
- 播放失败反馈。
- 角色开始回位。
- 主动触发原版钓鱼失败流程。

注意：

- 不要恢复为普通钓鱼条。
- 不要让玩家在失败后继续尝试当前这条鱼。
- 失败的核心目标是“接回原版失败结算”，不是“恢复原版小游戏控制权”。

### 8.7 `Returning`

返回岸上位置。

进入时：

- 让角色视觉位置插值回原位。
- 清掉跳水专用遮罩和临时状态。

退出时：

- 彻底清空 `DiveSlapSession`

## 9. 推荐的成功结算方式

### 9.1 第一选择：推进原版 `BobberBar`

成功后，优先让原版在下一次更新中完成成功结算。

推荐流程：

1. 将 `BobberBar.distanceFromCatching` 设置到成功阈值附近或以上。
2. 解除对 `BobberBar.update` 的冻结。
3. 让原版自然进入钓到鱼的流程。

注意：

- 实装前应先确认原版 `BobberBar.update` 的成功阈值。
- 如果阈值判断不是简单的 `>= 1f`，应在实现时以反编译代码为准。

### 9.2 关于 `perfect`

推荐第一版做法：

- 跳水成功后将 `BobberBar.perfect = false`

原因：

- 这是额外机制，不应顺便白拿完美钓鱼奖励。
- 这样更容易平衡，不至于把娱乐机制变成收益机制。

如果想更偏爽玩法，也可以保留原值，但要明确这是设计选择。

### 9.3 关于宝箱

推荐：

- 保留进入跳水前已获得的 `treasureCaught`
- 不在跳水阶段额外新增宝箱判定

### 9.4 推荐的失败结算方式

失败后，优先让原版在同一条失败链路内完成收尾。

推荐流程：

1. `DiveSlapController` 将 session 标记为 `ResolveFail`。
2. `DiveSlapRenderer` 执行回位或失败演出。
3. `VanillaFishingBridge` 请求触发原版失败。
4. `BobberBarPatch` 在合适的更新节点注入原版失败分支。
5. 清理当前 session。

实现要求：

- 失败后不再继续当前鱼的 `BobberBar` 捕获过程。
- 失败后不允许重新回到 `Slapping` 或普通钓鱼条。
- 失败后的 UI 和结果应尽量接近原版鱼逃跑体验。

注意：

- 具体失败入口需要以反编译 `BobberBar.update` 结果为准。
- 如果找到了可复用的原版失败方法，优先直接调用。
- 如果没有，就用 patch 注入同等行为，不要只做近似模拟。

## 10. 推荐的视觉实现

### 10.1 第一版：假跳水

目标：

- 让玩家“看起来”跳到了鱼钩附近。
- 让玩家“看起来”半身在水里。

实现方式：

1. 读取 `rod.bobber.Get()` 作为水中目标点。
2. 计算一个略低于鱼钩的农夫渲染点。
3. 在 `RenderedWorld` 中将农夫绘制到该位置。
4. 用水面矩形、水花粒子、泡沫特效遮住下半身。

这比真实裁剪更稳，且足够达到娱乐效果。

### 10.2 第二版：真半身裁剪

如果第一版视觉效果不够，再考虑：

- patch `FarmerRenderer.draw`
- 或者手动调用农夫绘制路径并裁掉下半部分

这属于增强项，不应阻塞第一版交付。

### 10.3 推荐层级处理

建议的绘制顺序：

1. 世界
2. 玩家或跳水农夫
3. 水花粒子
4. 水面遮罩
5. HUD / `BobberBar` 上层文本

这样更容易制造“下半身被水面盖住”的效果。

## 11. 输入与配置建议

推荐在 `ModConfig` 中增加以下字段：

```csharp
public KeybindList DiveSlapKey { get; set; } = KeybindList.Parse("MouseRight, Space");
public bool EnableDiveSlap { get; set; } = true;
public int DiveSlapDurationTicks { get; set; } = 90;
public int DiveSlapRequiredHits { get; set; } = 5;
public bool CancelPerfectOnDiveSuccess { get; set; } = true;
```

如果不想新增按键，也可以让 `SlapKey` 兼任：

- 钓到鱼后扇一下
- 钓鱼过程中触发跳水

但如果这么做，建议同步修改配置文案，让含义变成更通用的“扇鱼动作键”。

## 12. 异常与边界处理

以下情况必须直接取消或禁止跳水：

- 当前菜单不是 `BobberBar`
- `FishingRod` 丢失或切换了工具
- 玩家传送、睡觉、进入剧情
- 当前地点不允许正常钓鱼状态
- 鱼钩坐标异常
- 当前会话已经结束但没有正确清理

### 12.1 鱼钩坐标合法性

开始跳水前建议校验：

- `TargetBobberPosition` 是否在当前地图范围内
- 所在 tile 是否是水 tile

可使用：

- `GameLocation.isWaterTile(int xTile, int yTile)`

如果检查失败：

- 不启动跳水
- 继续保持原版钓鱼条，不进入跳水态

### 12.2 多人模式

第一版建议：

- 仅对本地玩家启用
- 不尝试同步跳水演出给其他玩家

多人兼容可以后续再做。

## 13. 推荐开发顺序

### 阶段 1：状态骨架

目标：

- 先把会话对象、状态机、配置项接好
- 不做复杂渲染

完成标准：

- 玩家在 `BobberBar` 中按键可进入一个“跳水状态”
- 屏幕能显示调试文字

### 阶段 2：冻结原版钓鱼条

目标：

- 跳水阶段激活时，原版 `BobberBar` 不再推进

完成标准：

- 进入跳水后，鱼条不会继续自动输赢

### 阶段 3：跳向鱼钩演出

目标：

- 角色在视觉上从岸边跳到鱼钩附近

完成标准：

- 可以看到明显的跳跃和位移效果

### 阶段 4：扇击计数

目标：

- 在限定时间内按键累计扇击数

完成标准：

- UI 能显示 `当前次数 / 目标次数`
- 成功和失败分支都能进入

### 阶段 5：接回原版结算

目标：

- 成功后钓到鱼
- 失败后直接进入原版失败流程

完成标准：

- 成功时确实进入钓到鱼后的展示或收杆流程
- 失败时确实走到原版失败收尾
- 失败时不会卡死在菜单中

### 阶段 6：视觉润色

目标：

- 水面遮罩
- 更好的粒子
- 更好的 punch 帧
- 提示文案和音效

## 14. 手动测试清单

至少测试以下场景：

- 普通鱼，正常触发跳水成功
- 普通鱼，跳水失败后直接进入原版失败流程
- 已接近成功时触发跳水
- 已接近失败时触发跳水
- 带宝箱的鱼
- 传说鱼或高难度鱼
- 刚开始钓鱼条就触发跳水
- 跳水中关闭菜单或状态异常中断
- 钓鱼地点靠边、狭窄水域、桥边
- 雨天、夜晚、不同地图

## 15. agent 实现注意事项

### 15.1 不要直接从“真水中移动”起步

先做：

- 状态机
- 假跳水渲染
- 原版结算接回

再考虑更复杂的真实移动。

### 15.2 不要一开始就重写 `BobberBar.draw`

优先：

- 让原版菜单继续画
- 用 `RenderedActiveMenu` 叠加自定义提示

这样成本最低，回退最容易。

### 15.3 不要自己决定鱼种结果

优先使用原版已有结果，除非已经确认原版结算无法复用。

### 15.4 每个阶段都要能独立验证

不要一次性写完全部逻辑再测试。

推荐每个阶段都加临时日志：

- 进入跳水
- 记录鱼钩位置
- 扇击计数变化
- 成功推进原版结算
- 失败触发原版失败结算
- 会话清理完成

## 16. 建议的第一版交付定义

如果目标是尽快做出可玩的版本，建议第一版只满足以下条件：

- 在 `BobberBar` 中按键可触发跳水扇鱼机制
- 角色看起来跳到鱼钩附近
- 有独立的倒计时和扇击计数
- 成功后能稳定钓上鱼
- 失败后能稳定进入原版钓鱼失败流程
- 视觉上通过水花和遮罩表现“半身入水”

以下内容可延后：

- 真正的半身裁剪
- 泳装切换
- 多人同步
- 更复杂的失败惩罚
- 更复杂的平衡参数

## 17. 结论

推荐实现路线是：

“假跳水演出 + 独立扇击状态机 + 暂停原版钓鱼条 + 成功后借原版成功结算 + 失败后接原版失败结算”

这条路线的优点是：

- 技术风险最低
- 与当前 mod 架构最匹配
- 最适合娱乐性 mod
- 可先做出好玩的版本，再逐步增强视觉细节

对于本项目，不推荐第一版就追求“真实下水、真实碰撞、真实裁剪”。先把玩法做通，比把渲染做满更重要。
