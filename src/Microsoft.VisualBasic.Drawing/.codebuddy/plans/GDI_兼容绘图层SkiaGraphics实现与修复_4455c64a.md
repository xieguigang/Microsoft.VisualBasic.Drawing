---
name: GDI+兼容绘图层SkiaGraphics实现与修复
overview: 审查并修复 Microsoft.VisualBasic.Drawing 中基于 SkiaSharp 的 GDI+ 兼容绘图层（SkiaGraphics 及其子类），提出与 GDI+ 行为不一致的修改方案，并实现全部 45 处 NotImplementedException 绘图函数，同时新增 Brush→SKPaint 转换助手以支持非纯色画刷。
todos:
  - id: fix-fill-ellipse-and-brush-helper
    content: 修复 FillEllipse 矩形坐标错位，并在 Interop.vb 新增 Brush.CreatePaint 助手且重构 Fill* 使用
    status: completed
  - id: implement-curves-and-pie
    content: 实现 DrawBezier/DrawBeziers/DrawCurve 全重载/DrawClosedCurve/FillClosedCurve/DrawPie/DrawRectangles（含 Catmull-Rom 助手）
    status: completed
    dependencies:
      - fix-fill-ellipse-and-brush-helper
  - id: implement-image-mapping
    content: 实现 DrawImage 映射点仿射绘制与 DrawImageUnscaledAndClipped 裁剪绘制
    status: completed
    dependencies:
      - fix-fill-ellipse-and-brush-helper
  - id: implement-transform-and-clip
    content: 实现坐标变换、裁剪区域管理、IsVisible 与 AddMetafileComment 占位
    status: completed
    dependencies:
      - fix-fill-ellipse-and-brush-helper
  - id: verify-build-warnings
    content: 编译验证 OptionStrict 与 WarningsAsErrors 通过，检查未用变量/隐式转换
    status: completed
    dependencies:
      - fix-fill-ellipse-and-brush-helper
      - implement-curves-and-pie
      - implement-image-mapping
      - implement-transform-and-clip
---

## 用户需求

审查现有 GDI+ 兼容绘图代码，找出与 GDI+ Graphics 绘图结果/行为不一致的实现并提出修改方案；并尝试实现当前所有 `Throw New NotImplementedException()` 尚未实现的函数。

## 产品概述

本任务面向 VB.NET + SkiaSharp 的 GDI+ 兼容绘图库（在 Linux 上替代 .NET 已废弃的 GDI+）。需在抽象基类 `SkiaGraphics` 中补齐缺失的绘图/变换/裁剪能力，并修正一处明显坐标错位 BUG，使基于 GDI+ 函数签名的调用在 Skia 后端得到一致、可预期的渲染结果。修改将自动惠及 `Graphics`（栅格）、`PdfGraphics`、`SvgGraphics` 三个子类。

## 核心功能

- 修复 `FillEllipse(rect)` 的坐标错位 BUG（矩形中心被误当作左上角）。
- 实现贝塞尔曲线（DrawBezier/DrawBeziers）、Catmull-Rom 样条曲线（DrawCurve 全重载、DrawClosedCurve/FillClosedCurve）与饼图扇形（DrawPie 全重载）、矩形数组（DrawRectangles）。
- 实现图像仿射映射（DrawImage 按映射点绘制）与裁剪式图像绘制（DrawImageUnscaledAndClipped）。
- 实现坐标变换（TranslateTransform/ScaleTransform/RotateTransform/ResetTransform）与裁剪区域管理（SetClip/IntersectClip/ExcludeClip/ResetClip/TranslateClip）。
- 实现 `IsVisible` 可见性判断与 `AddMetafileComment` 兼容性占位（无操作 + 警告）。
- 新增 `Brush→SKPaint` 转换助手，支持纯色/线性渐变/纹理画刷（不可用时优雅降级），并重构 Fill* 方法使用之。
- 对 DrawString 基线、Pen.CreatePaint 虚线/线帽、PathBuilder 弧度/样条、MeasureString 换行等潜在不一致，于技术章节给出修改方案（按用户确认，本次仅提议，不修改代码）。

## 技术栈

- 语言/框架：VB.NET，目标框架 net10.0（项目文件声明），`OptionStrict On` 且 `WarningsAsErrors` 含 42024/42030/42099/42104-42107/42353/42354（禁止隐式转换/未用变量等）。
- 图形后端：SkiaSharp 4.150.0（含 SkiaSharp.Extended 用于 SVG），已有 `Microsoft.VisualBasic.Imaging` 项目引用提供 Pen/Brush/GraphicsPath/Font/Image 等类型。
- 不引入新依赖；复用现有扩展 `pen.CreatePaint`、`AsRectangle`、`AsSKPoint`、`AsSKColor`、`PathBuilder.CreatePath`。

## 实现策略

所有绘图/变换/裁剪逻辑统一在抽象基类 `SkiaGraphics` 中基于 `m_canvas`（SKCanvas）、`SKPath`、`SKMatrix` 实现，三个子类自动继承，避免重复与行为分叉。

### A. 本次修复的明显 BUG（代码修改）

1. **FillEllipse 矩形坐标错位**（SkiaGraphics.vb 480-490）：`FillEllipse(brush, rect As Rectangle/RectangleF)` 现用 `rect.Centre` 作为 `x,y` 传入 `FillEllipse(brush, x, y, width, height)`，而 Skia `DrawOval(x,y,w,h)` 的 `(x,y)` 是左上角，导致椭圆被错误平移。修复为传入 `rect.Left, rect.Top`（Rectangle 与 RectangleF 两处均改），与 GDI+ 以包围矩形左上角为基准一致。

### B. 本次实现的 NotImplementedException 分组

- **曲线与饼图**：
- `DrawBezier`：构造 `SKPath`（`MoveTo(pt1)` + `CubicTo(pt2,pt3,pt4)`）后用 `pen.CreatePaint` 描边。
- `DrawBeziers`：相邻 4 点一组连续 `CubicTo`（首点 `MoveTo`），还原 GDI+ 连续贝塞尔行为。
- `DrawCurve`/`DrawClosedCurve`/`FillClosedCurve`：新增私有助手，将 N 个点 + tension（默认 0.5，对应 GDI+ 默认张力）转换为 Catmull-Rom 样条，以 `CubicTo` 段生成 `SKPath`；`DrawCurve(points, offset, numberOfSegments, [tension])` 取子段 `[offset .. offset+numberOfSegments]`；闭合版本追加 `Close()`。`FillClosedCurve` 用 `brush.CreatePaint` 填充。
- `DrawPie`：构造扇形轮廓 `SKPath`——`ArcTo(oval, startAngle, sweepAngle, forceMoveTo:=True)`（弧起点 MoveTo、弧到终点）后 `LineTo(cx, cy)`（弧终点到圆心）再 `Close()`，用 `pen.CreatePaint` 描边。GDI+ 角度约定（0°=3 点方向、顺时针为正）与 Skia 一致。
- **矩形数组**：`DrawRectangles` 循环调用现有 `DrawRectangle(pen, rect)`。
- **图像映射**：
- `DrawImage(image, destPoints() As Point(F))`：GDI+ 将源矩形 (0,0,w,h) 仿射映射到由映射点定义的平行四边形（3 点：左上/右上/左下）或四边形（4 点）。计算 `SKMatrix`：`m_canvas.Save()` → `SetMatrix(SKMatrix.Concat(current, map))` → 以原始尺寸在 (0,0) 绘制 → `Restore()`。3 点走仿射；4 点优先走仿射（必要时标注透视限制）。
- `DrawImageUnscaledAndClipped(image, rect)`：先 `ClipRect(rect.AsRectangle)` 后再以原始尺寸于 (0,0) 绘制（或 Save/Clip/Restore）。
- **坐标变换**：在基类保存构造期矩阵 `_initialMatrix = m_canvas.GetMatrix()`（通常为 Identity）。`TranslateTransform/ScaleTransform/RotateTransform` 调用 `m_canvas.Translate/Scale/RotateDegrees`；`ResetTransform` 还原到 `_initialMatrix`。
- **裁剪与可见性**：
- 引入受保护字段跟踪裁剪基线（`_clipBaseSaveCount`）。`SetClip(rect)`：`Save()` 后 `ClipRect(rect.AsRectangle, SKClipOperation.Replace, True)`；`IntersectClip` 用 `SKClipOperation.Intersect`；`ExcludeClip` 用 `SKClipOperation.Difference`；`ResetClip` 恢复到 `_clipBaseSaveCount` 对应 `Restore()`。
- `TranslateClip(dx,dy)`：Skia 裁剪为设备空间、无直接平移 API；若当前为单一矩形裁剪则按平移后矩形重 `SetClip`，否则记录偏移并在后续裁剪应用（于方案中标注近似限制）。
- `IsVisible`：近似以 `rect.IntersectsWith(canvasRect)` 判断，若已启用裁剪跟踪则再与当前裁剪区求交，返回 Boolean。
- **AddMetafileComment**：GDI+ 仅对图元文件有意义，Skia 栅格/PDF/SVG 无对应概念；保持 API 兼容，执行无操作并输出一条 warning 日志，不抛异常。

### C. Brush→SKPaint 转换助手（q2，代码修改）

在 `Interop/Interop.vb` 新增扩展 `Brush.CreatePaint() As SKPaint`：用 `TryCast` 探测 `SolidBrush`（取 `Color.AsSKColor`）、`LinearGradientBrush`（尝试反射读取渐变端点/颜色，`SKShader.CreateLinearGradient`）、`TextureBrush`（尝试反射读取图像，`SKShader.CreateImage`）；任一不匹配则优雅降级（warning 并返回默认黑/跳过），绝不抛 `InvalidCastException`。将 `FillEllipse/FillRectangle/FillPolygon/FillPath` 中的 `DirectCast(brush, SolidBrush).Color.AsSKColor` 改为 `brush.CreatePaint()`；新建的 `FillClosedCurve`、潜在 `DrawPie` 填充等统一使用。

### D. 仅提议、本次不修改代码的潜在不一致（写入方案，供后续迭代）

1. **DrawString 基线**（SkiaGraphics.vb 53-71）：Skia `DrawText` 的 y 为基线，而 GDI+ `(x,y)` 为文字包围盒左上角；当前用 `y + textBounds.Height` 过度下移。提议基线取 `y + fontMetrics.Ascent`。另 63-66 行在 61 行 `SKTextBlob.Create(s, skfont)` 之后才判断 `s Is Nothing`，应前移判断避免 `Nothing` 抛异常。
2. **Pen.CreatePaint 缺失虚线/线帽/线连接**（Interop.vb 114-122）：仅设置 Color/IsAntialias/Style/StrokeWidth，导致 `DrawArc/DrawPath/DrawEllipse` 等忽略 `pen.LineDashStyle`(PathEffect)、`StrokeCap`、`StrokeJoin`、`StrokeMiter`。提议补全这些属性（默认 Butt/Miter 对齐 GDI+）。
3. **PathBuilder.AddArc 传入弧度**（PathBuilder.vb 79-93）：`skia.ArcTo(oval, radians, radians, False)` 但 Skia `ArcTo` 期望角度为度，且 `forceMoveTo=False` 会连线而非 MoveTo。提议传角度 + `forceMoveTo=True`。
4. **PathBuilder.AddCurve 用折线代替样条**（PathBuilder.vb 68-70）：`AddPoly` 丢失 Catmull-Rom 插值，提议转换为 `CubicTo` 段（与 D 部分曲线助手复用）。
5. **MeasureString 忽略 width/layoutArea**（SkiaGraphics.vb 652-662 / Driver.vb 23-33）：GDI+ 会按宽度自动换行并返回含 padding 包围盒；当前仅测单行 +1。提议实现换行与 padding。

## 实现注意（防回归与性能）

- 严格遵循 `OptionStrict On`：所有 `TryCast` 后判 `IsNot Nothing`；矩阵/路径用 `Using` 释放 `SKPath`/`SKPaint`/`SKImage`，与现有 `Using` 风格一致。
- 变换/裁剪使用 `Save/Restore` 配对，避免污染后续绘制的全局画布状态（现有 `DrawString` 角度版已存在泄漏风险，新代码务必成对）。
- 不调整未涉及的逻辑；`AddMetafileComment`、`TranslateClip` 等做安全降级，避免引入破坏性变更。
- 复用 `pen.CreatePaint` 与新增 `brush.CreatePaint`，不重复构造 paint 逻辑，保持单一来源。
- 性能：曲线/样条仅对输入点集做一次采样，避免冗余遍历；图像映射仅一次 `SetMatrix`。

## 架构与目录结构

本任务为对既有基类的增量补充，不涉及新架构模式；改动集中在两个文件，三个子类自动受益。

```
src/Microsoft.VisualBasic.Drawing/
├── SkiaGraphics.vb        # [MODIFY] 修复 FillEllipse(rect) 坐标错位；实现 DrawBezier/DrawBeziers/DrawCurve(全重载)/DrawClosedCurve/FillClosedCurve/DrawPie(全重载)/DrawRectangles/DrawImage(映射点)/DrawImageUnscaledAndClipped/TranslateTransform/ScaleTransform/RotateTransform/ResetTransform/SetClip/IntersectClip/ExcludeClip/ResetClip/TranslateClip/IsVisible/AddMetafileComment；新增 Catmull-Rom 样条私有助手与 _initialMatrix/_clipBaseSaveCount 字段；Fill* 改用 brush.CreatePaint。
└── Interop/
    └── Interop.vb         # [MODIFY] 新增扩展 Brush.CreatePaint（SolidBrush/LinearGradientBrush/TextureBrush 探测 + 优雅降级）；保留 Pen.CreatePaint 现状（其增强仅作提议）。
```

## 关键代码结构（签名级）

```
' Interop/Interop.vb —— 画刷到 SKPaint 的转换助手（优雅降级，不抛异常）
<Extension>
Public Function CreatePaint(brush As Brush) As SKPaint
' TryCast SolidBrush -> 纯色; LinearGradientBrush -> CreateLinearGradient; TextureBrush -> CreateImage; 否则 warning 降级

' SkiaGraphics.vb —— Catmull-Rom 样条助手（tension 默认 0.5，对应 GDI+ 张力）
Private Function BuildSplinePath(points() As PointF, tension As Single, closed As Boolean) As SKPath
```