using StereoKit;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TouchMenuApp
{
    public class App
    {
        [StructLayout(LayoutKind.Sequential)]
        struct ButtonData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public Vec4[] button;
        }
        ButtonData buttons;

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
        Vec4 testVec4 = new Vec4(0.33f, 0.16f, 0, 0);
        int buttonAmount = 3;

        public void Init()
        {
            buttons.button = new Vec4[10] { new Vec4(0.5f, 0.16f, 0, 0),
                                            new Vec4(0.33f, 0.26f, 0, 0),
                                            testVec4,
                                            testVec4,
                                            testVec4,
                                            testVec4,
                                            testVec4,
                                            testVec4,
                                            testVec4,
                                            testVec4 };

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

            float longestSide = FindLongestSide(touchPanel);

            var i = 0;
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

                    buttons.button[i] = new Vec4(_positionX, _positionY, 0, 0);
                    //buttonPosList.Add(new Vec2(_positionX, _positionY));
                    i++;
                    System.Console.WriteLine(item.Name + "  Original x pos: " + item.LocalTransform.Pose.position.x + "  Tweaked: " + _positionX);
                }
            }

            foreach (var item in buttons.button)
            {
                System.Console.WriteLine(item.x);
            }

            touchPanelMat.SetData<ButtonData>("button", buttons);
            touchPanelMat.SetInt("buttonAmount", i);
        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            UI.Handle("Cube", ref cubePose, touchPanel.Bounds);
            touchPanel.Draw(cubePose.ToMatrix());


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