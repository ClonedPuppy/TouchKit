using StereoKit;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TouchMenuApp
{
    public class App
    {
        //[StructLayout(LayoutKind.Sequential)]
        //struct ButtonData
        //{
        //    public Vec2 button01;
        //    public Vec2 button02;
        //    public Vec2 button03;
        //    public Vec2 button04;
        //    public Vec2 button05;
        //    public Vec2 button06;
        //    public Vec2 button07;
        //    public Vec2 button08;
        //    public Vec2 button09;
        //    public Vec2 button10;
        //}

        //Vec2[] buttons = new Vec2[10];

        [StructLayout(LayoutKind.Sequential)]
        struct ButtonData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public Vec2[] buttons = new Vec2[10];
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SliderData
        {
            public Vec3 slider01;
            public Vec3 slider02;
            public Vec3 slider03;
            public Vec3 slider04;
            public Vec3 slider05;
            public Vec3 slider06;
            public Vec3 slider07;
            public Vec3 slider08;
            public Vec3 slider09;
            public Vec3 slider10;
        }

        //ButtonData buttons;
        SliderData sliders;

        public SKSettings Settings => new SKSettings
        {
            appName = "TouchMenu",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.MixedReality
        };

        Pose cubePose = new Pose(0, 0, -0.5f, Quat.FromAngles(90, 0, 0));
        Model touchPanel;
        Model sphere;

        Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
        Tex touchPanelTex;
        Shader touchPanelShader;
        Material floorMaterial;
        Material touchPanelMat;

        List<Vec2> buttonPosList = new List<Vec2>();
        int buttonAmount = 3;

        public void Init()
        {

            sliders = new SliderData
            {
                slider01 = new Vec3(0.2f, 0.33f, 0.16f),
                slider02 = new Vec3(0.2f, 0.5f, 0.5f),
                slider03 = new Vec3(0.2f, 0.5f, 0.5f),
                slider04 = new Vec3(0.2f, 0.0738f, 0.05f),
                slider05 = new Vec3(0.2f, 0.008f, 0.17f),
                slider06 = new Vec3(0.2f, -0.182f, 0.62f),
                slider07 = new Vec3(0.2f, 0f, 0f),
                slider08 = new Vec3(0.2f, 0.70710677f, 0f),
                slider09 = new Vec3(0.2f, 0.7071067f, 1f),
                slider10 = new Vec3(0.2f, 0.7071067f, 1f)
            };

            // Create assets used by the app
            sphere = new Model(Mesh.Sphere, Material.Default);

            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            touchPanel = Model.FromFile("Panel_v001.glb");
            touchPanelTex = Tex.FromFile("TouchMenuTexture.tga");
            touchPanelShader = Shader.FromFile("TouchPanelShader.hlsl");
            touchPanelMat = new Material(touchPanelShader);
            touchPanelMat[MatParamName.DiffuseTex] = touchPanelTex;
            touchPanelMat.Transparency = Transparency.Blend;
            touchPanel.Visuals[0].Material = touchPanelMat;

            //button01 = touchPanel.FindNode("button01");

            float longestSide = FindLongestSide(touchPanel);

            //FieldInfo[] members = buttons.GetType().GetFields();

            //object tempValueToAssign = "A test string";

            //foreach (FieldInfo fi in members)
            //{
            //    // perform update of FieldInfo fi
            //    fi.SetValue(buttons, tempValueToAssign);
            //}

            int i = 0;
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name == "panel")
                {
                    System.Console.WriteLine("nope"); ;
                }
                else
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;


                    buttonPosList.Add(new Vec2(_positionX, _positionY));

                    System.Console.WriteLine(item.Name + "  Original x pos: " + item.LocalTransform.Pose.position.x + "  Tweaked: " + _positionX);
                }
            }

            //buttons = new ButtonData
            //{
            //    button01 = buttonPosList[0],
            //    button02 = buttonPosList[1],
            //    button03 = buttonPosList[2],
            //    button04 = buttonPosList[3],
            //    button05 = buttonPosList[4],
            //    button06 = buttonPosList[5],
            //    button07 = buttonPosList[6],
            //    button08 = buttonPosList[7],
            //    button09 = buttonPosList[8],
            //    button10 = buttonPosList[9]
            //};
        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            UI.Handle("Cube", ref cubePose, touchPanel.Bounds);
            touchPanel.Draw(cubePose.ToMatrix());

            touchPanelMat.SetData<ButtonData>("buttons", buttons);
            touchPanelMat.SetInt("buttonAmount", buttonAmount);
        }

        float FindLongestSide(Model model)
        {
            if (model.Bounds.dimensions.x < model.Bounds.dimensions.z)
            {
                return model.Bounds.dimensions.z;
            }
            else
            {
                return model.Bounds.dimensions.x;
            }
        }
    }
}