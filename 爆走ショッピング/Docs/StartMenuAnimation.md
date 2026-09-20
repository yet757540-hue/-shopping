# 开始界面动效

打开 `Assets/_Project/Scenes/Menu/MainMenu.unity`，进入 Play Mode 即可播放。沿用项目内已抠底、裁切的 TitleLogo 与 Character 素材。

- 标题：从右侧屏幕外向左冲入，横向拉伸、纵向压缩，轻微越过落点后回弹；约 0.62 秒落定。取消标题淡入，让冲入过程清晰可见。
- 胶片：沿用上下角胶片的 Repeat 纹理 UV 循环，不移动胶片边框；默认速度 340 基准像素/秒。
- 清理旧效果：关闭标题多层彩色复制形成的伪描边，保留素材原有白边；胶片孔使用等宽等高的直角正方形，无圆角。
- 底部按钮：開始、設定、終了依次冲入，间隔 0.12 秒；入场结束后小幅上下起伏、倾斜。选中时平滑位移、放大并轻微呼吸，按下时收缩。
- 动效使用非缩放时间；返回设置菜单后不会重新播放入场。按钮视觉子节点动画不改变交互根节点的点击区域。

在 MainMenu 场景的 StartMenuManager → Theme 中调整：

| 设置 | 用途 |
| --- | --- |
| Title Rush / Start Outside Canvas | 根据画布、缩放、旋转和轴心保证从右侧屏幕外出发 |
| Title Rush / Duration、Overshoot | 冲入时长、回弹幅度 |
| Top / Bottom Film Strip / Scroll Speed、Scroll Direction | 胶片速度与方向 |
| Cart Rush、Cart Stagger | 按钮入场和错峰间隔 |
| Cart Buttons / Idle Motion、Idle Bob、Idle Tilt、Idle Period | 待机动效开关、起伏、倾斜和周期 |
| Cart Buttons / Selected Pulse、Response Speed | 选中呼吸幅度和交互过渡速度 |

代码默认值与场景设置已同步。编辑器的 `Tools/Start Screen/Run Play Mode Smoke Test` 检查入场落定、标题屏幕外起点与重播位置、胶片滚动、按钮持续运动及菜单交互；结果输出到 `Logs/start-menu-smoke-test.txt`。该测试结束后会退出编辑器，请先保存工作。
