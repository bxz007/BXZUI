# Procedure 使用文档

简述：该工具用于Editor下可视化配置 节点对节点数据，可以方便的自定义节点以及自定义流程策略，目前可以在新手引导、功能引导、或者在游戏流程控制中使用该工具。



## 快速开始

- **创建数据（demo: Resources/Procedure）**

  - 在Project面板，右键选择当前文件夹 [@ProcedureCanvas/Create](#)，会生成一个名为Procedure.asset的数据文件，**选中文件，修改StrategyType = 2 (Novice)**

  - 右键选中生成的Procedure文件，[@ProcedureCanvas/Open]() 打开数据。**也可以不选中文件，默认打开上一次的Procedure数据**

  - 打开后可以看到一个空的面板，左侧为当前策略所有流程、中间空白区域用于节点对节点流程配置，最右侧为当前所有自定义的节点

    ![Image](Editor/Doc~/Step0.png)

    

    

- **开始配置（支持运行时配置,Demo场景 : SampleScene）**

  - 下面我们开始配置一段流程，①进入游戏后显示弹窗"文案1" => ②加载预制体Page1 => ③等待0.3S => ④引导点击page1中的进确定按钮 =>⑤判断值等于0 或者 1 => ⑥显示弹窗"文案2"/"文案3“ => ⑦加载预制体Page2 => ⑧引导点击比赛按钮 => ⑨显示弹窗”文案4“ =>流程结束

    

    启动游戏，并保持Procedure窗口激活状态, 在左下角添加一个ID为20的流程，并单击选中

    ① 在右侧节点列表中选择Dialogue/Dialogue,通过拖拽方式拖到Start附近，按下start推拽连线选中Dialogue，Dialogue节点中填入数据"文案1"，右键选中Dialogue节点，点击"从这里运行"，可以看到已经弹出文案1的对话框

    ![](Editor/Doc~/Step1.gif)

    ② 按步骤①的方式拖拽Specific tools中的LoadPrefab到Dialogue节点附近，按下Dialogue中的NEXTSTEP>链接到LoadPrefab节点，填入预制体路径和父节点到节点中,再次右键选中Dialogue，点击"从这里运行"，可以看到对话框弹出，点击后加载了Page1的预制体

    ![](Editor/Doc~/Step2.gif)

    ③ 如上拖拽步骤添加Time/Await节点，并填上0.3S

    ④ 添加Mask/MaskButton 节点，填入按钮运行时路径，可以在unity Hierarchy 面板右键选中按钮@CopyPath运行时路径（有父节点的情况），右键从这里运行MaskButton，可以看到场景中已经聚焦按钮

    ![](Editor/Doc~/Step3.gif)

    ⑤ 添加Specific tools/Step，并链接MaskButton到Step

    ![](Editor/Doc~/Step4.gif)

    ⑥添加 两个Dialogue，分别填入"文案2"/“文案3”,分别链接Step的link1和link2到两个Dialogue节点

    ....

    配置完成的数据如demo: Resources/Procedure

- **在代码中调用**

  ``` c#
  ProcedureMgr.Begin(20, Procedure.SoccerProcedureStrategy.Novice);
  ```

  第一个参数为流程ID,第二个是策略ID，下面是代码调用后执行的完整流程：

  ![](Editor/Doc~/Step6.gif)

Procedure中有三个[ID](#) : [StrategyID](#)，[ProcedureID](#), [ActionID](#)，分别对应流程策略，流程，节点，在开始之前，至少需要定义一个策略和一个节点。（以下为demo中的类型为例子）

- **如何定义策略**

  - 定义策略ID

    ```c#
    
        public class ProcedureStrategyType 
        {
            public const int None = 0;
            public const int Novice = 1 << 1;
            public const int Function = 1 << 2; 
        }
    ```

    使用2的幂作为值，方便使用位运算关闭多个策略

    

  - 定义策略类型

    ```c#
    [ProcedureStrategy(ProcedureStrategyType.Novice)]
    public class NoviceProcedure : StrategyBase
    {
    }
    ```
    继承自StrategyBase，并实现里面的抽象方法，加上ProcedureStrategyAttribute用于告诉管理器这个策略对应NoviceID。之后就是策略逻辑，以什么样的形式执行节点，demo中NoviceProcedure对应的新手引导策略，是以线性执行节点的形式执行流程。FunctionProcedure对应的是功能引导，可以多个流程同时执行

    

- **如何定义节点**

  - 定义节点ID

    ```c#
    /// <summary>
    /// 当前工程引导节点
    /// </summary>
    public class SoccerProcedureType : PartialBase
    {
        public SoccerProcedureType(int value) : base(value) { }
    
        public const int MaskButton = 0;   //遮罩按钮
        public const int Await = 1; //等待节点，可以选择是否遮罩
        public const int Dialogue = 2; //弹出对话框
        public const int LoadPrefab = 3; //加载预制体
    }
    ```
    定义节点ID需要继承自PartialBase，主要是用于编辑器下反射获取ID对应的常量名， MaskButton == 0 

    

  - 定义节点类型

    ```   c#
     [Tooltip("遮罩 仅当前按钮可以被点击")]
     [ActionFlag(typeof(SoccerProcedureType), SoccerProcedureType.MaskButton, "Mask")]
     public class MaskButtonActionNode : BaseActionNode
     {
           [Tooltip("按钮运行时路径")]
           [RequestField]
           public GString buttonPath;
           
           [Tooltip("按钮位置偏移")]
           [RequestField]
           public GVector2 offSet;
      }
          
    ```

    Tooltip 用于在编辑器中显示描述，ActionFlagAttribute用于编辑器主动识别ID、名字、分类。GString和Gvector2对应的是string和vector2，流程控制器中自定义了GString、GVector2、GBool、GColor、GFloat、GInt、GLink、GVector3 这8个基础类型（目前仅支持这些类型）

    

- **设置Loader**

   

  ```c#
  public class SoccerProjectLoader : Loader
  {
      [RuntimeInitializeOnLoadMethod]
      public static void Setloader()
      {
          LoaderUtil.Util = new SoccerProjectLoader();
      }
          
      public T Load<T>(string path) where T : Object
      {
          return Resources.Load<T>(path);
      }
  
      public void UnLoad<T>(Object @object, string path)
      {
          Resources.UnloadAsset(@object);
      }
  }
  ```
  需要按照代码中的SoccerProjectLoader定义一个当前工程loader，用工程自己的资源管理方式加载流程数据。默认是Resource.load

  

## 编辑器的快捷操作

| 按键           | 功能                       |
| -------------- | -------------------------- |
| 鼠标滚轮滚动   | 缩放节点页面               |
| 鼠标滚轮长按   | 拖拽节点页面               |
| Delete         | 删除选中节点               |
| Ctrl+Z         | 回滚链接、输入、删除、新增 |
| 右键选择节点   | 从这里开始运行             |
| 右键选中流程ID | 备注、修改ID、删除         |
| R              | 重置滚轮偏移为0            |
| H              | 开启跟随                   |

## 使用技巧

理论上在不新增节点的情况下，全流程支持热更新。基本上所有逻辑都可以抽成节点，如果遇到需求新增一个节点不能解决，那就增加两个节点 （0_0） 目前新足工程有58个大大小小的节点，都用于满足配置需求。附图

![](Editor/Doc~/StepLast.png)