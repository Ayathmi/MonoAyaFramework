一款基于R3和UniTask的Unity命令模式MVC框架，受QFramework启发
______
### 特性

+ 提供**异步**的命令模式 - 主要用途
+ 专属IoC容器
+ 轻量
+ 可基于约束(Constrains Region)接口轻松扩展

### 安装步骤

本MVC框架适用于Unity项目构建。

1. 向您的Unity项目中导入[NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)插件
2. 在NugetForUnity的Manager Nuget Packages面板中，查找并安装Nuget包”R3“

	**注意：第三步的UniTask不应从NugetForUnity中下载，该来源的包功能不完整，参见[此处](https://github.com/Cysharp/UniTask?tab=readme-ov-file#net-core)**
3. 从Github下载UniTask插件，并导入至您的Unity项目
4. 复制仓库中的[MonoAya.cs](https://github.com/Ayathmi/MonoAyaFramework/blob/main/MonoAya/Assets/Scripts/MonoAya.cs)到项目中

### 当前包含示例

1. UI管理器及少量测试用例
2. coming s∞n

### 更新计划

Coming s∞n（作者自己也在用这个框架，她会在后续使用中改进的，大概

### 已知问题

**注意：未经大批量测试**

本仓库未经有效健壮度验证，请勿用于生产环境，一切将本仓库代码用于生产环境所造成损失，作者概不负责

### 更新日志

N/A