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
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] button;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SliderData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] slider;
        }

        public SKSettings Settings => new SKSettings
        {
            appName = "TouchMenu",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.MixedReality
        };

        Pose cubePose = new Pose(0, 0, -0.4f, Quat.FromAngles(-90, 180, 0));
        //Pose cubePose = new Pose(0, 0, 0, Quat.Identity);
        Model touchPanel;
        Model sphere;

        Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
        Tex touchPanelTex;
        Shader touchPanelShader;
        Material floorMaterial;
        Material touchPanelMat;

        List<Vec4> buttonList = new List<Vec4>();
        List<Vec4> sliderList = new List<Vec4>();

        PushButton pushButton;

        public void Init()
        {
            ButtonData buttons = new ButtonData();

            SliderData sliders = new SliderData();

            pushButton = new PushButton();

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

            buttons.button = new Vec4[20];

            var i = 0;
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name == "panel")
                {
                    //System.Console.WriteLine("nope"); ;
                }
                else
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;

                    buttons.button[i] = new Vec4(_positionX, _positionY, 0, 0);
                    buttonList.Add(new Vec4(_positionX, _positionY, 0, 0));
                    i++;
                    //System.Console.WriteLine(item.Name + "  Original x pos: " + item.ModelTransform.Pose.position.x + "  Tweaked: " + _positionX);
                    //System.Console.WriteLine(item.ModelTransform.Pose.position.z);
                }
            }

            touchPanelMat.SetInt("buttonAmount", i);
            touchPanelMat.SetData<ButtonData>("button", buttons);
            if (SK.ActiveDisplayMode == DisplayMode.Flatscreen)
            {
                //Renderer.CameraRoot = Matrix.TR(V.XYZ(0, 0.5f, 0.185f), Quat.FromAngles(-69, 0, 0));
            }

        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            UI.Handle("Cube", ref cubePose, touchPanel.Bounds);
            touchPanel.Draw(cubePose.ToMatrix());

            Hierarchy.Push(cubePose.ToMatrix());
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name == "panel")
                {
                    //System.Console.WriteLine("nope"); ;
                }
                else
                {
                    pushButton.Button(touchPanel, item.Name, false);
                    sphere.Draw(Matrix.TS(item.ModelTransform.Pose.position, 0.01f));
                }
            }
            Hierarchy.Pop();
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