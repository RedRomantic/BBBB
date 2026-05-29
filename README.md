# 全景合约市场 AI 决策系统

基于 .NET 8、WPF 和 MVVM 架构开发的加密货币合约市场分析与 AI 决策系统。

## 功能特性

- **实时市场数据获取**: 并行获取币安 USD-M 永续合约市场数据
- **量化指标计算**: BBW, ATR, ADX, EMA, RSI 等技术指标
- **宏观市场阶段分析**: 四阶段市场周期自动识别
- **AI 策略生成**: 基于 OpenAI GPT 模型的流式输出策略建议

## 项目结构

```
PanoramaFuturesAI/
├── Commands/           # 命令实现 (MVVM)
├── Models/             # 数据模型
├── Services/           # 业务服务
├── Utils/              # 工具类
├── ViewModels/         # 视图模型
├── Views/              # 视图层
├── App.xaml           # 应用程序入口
└── PanoramaFuturesAI.csproj
```

## 技术栈

- .NET 8
- WPF (Windows Presentation Foundation)
- CommunityToolkit.Mvvm
- Skender.Stock.Indicators
- HttpClient (原生，无第三方 HTTP 库)

## 界面布局

```
┌─────────────┬─────────────┬─────────────┐
│  AI策略输出 │ 市场归因    │ 全局市场   │
│  (A区 45%) │ (B/B2/C 30%)│ 趋势看板   │
│             │             │ (D区 25%) │
│             │ - 市场归因   │            │
│             │ - 关键指标   │            │
│             │ - LLM控制    │            │
└─────────────┴─────────────┴─────────────┘
```

## 市场阶段定义

| 阶段 | 名称 | 特征 |
|------|------|------|
| 阶段一 | 低波蓄势期 | BBW 极低，ADX < 20 占比高 |
| 阶段二 | 高波突破期 | BBW 扩大，成交量激增 |
| 阶段三 | 趋势主升/主跌期 | ADX > 25，均线多头/空头排列 |
| 阶段四 | 趋势衰竭期 | 价格偏离 EMA20，RSI 极值 |

## 编译运行

```bash
# 还原依赖
dotnet restore

# 编译
dotnet build

# 运行
dotnet run
```

## 使用说明

1. 启动程序后自动加载市场数据
2. 在 C 区输入 OpenAI API Key
3. 选择模型（gpt-4 等）
4. 点击"刷新数据"更新市场分析
5. 点击"生成策略"获取 AI 策略建议

## 注意事项

- 需要稳定的网络连接访问币安 API
- AI 策略功能需要有效的 OpenAI API Key
- 合约数量建议设置为 20-50 个以平衡性能和覆盖率

## License

MIT License
