using StereoKit;
using System.Collections.Generic;
using System.Linq;
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
        Model touchPanel;

        Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
        Tex touchPanelDiff;
        Tex touchPanelMRAO;
        Shader touchPanelShader;
        Material floorMaterial;
        public static Material touchPanelMat;

        List<Vec4> buttonList = new List<Vec4>();
        List<Vec4> sliderList = new List<Vec4>();

        PushButton pushButton;

        public void Init()
        {
            Renderer.SkyTex = Tex.FromCubemapEquirectangular(@"Container_Env.hdr");
            Renderer.SkyTex.OnLoaded += t => Renderer.SkyLight = t.CubemapLighting;
            Renderer.EnableSky = true;

            ButtonData buttons = new ButtonData();

            SliderData sliders = new SliderData();

            pushButton = new PushButton();

            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            touchPanel = Model.FromFile("Panel_v001.glb");
            touchPanelDiff = Tex.FromFile("TouchMenuDiffuse.png");
            touchPanelMRAO = Tex.FromFile("TouchMenuMRAO.png");
            touchPanelShader = Shader.FromFile("simple.hlsl");
            touchPanelMat = new Material(touchPanelShader);
            touchPanelMat[MatParamName.DiffuseTex] = touchPanelDiff;
            touchPanelMat[MatParamName.MetalTex] = touchPanelMRAO;
            touchPanelMat.Transparency = Transparency.Blend;
            touchPanel.Visuals[0].Material = touchPanelMat;

            // Is the panel landscape or portrait?
            float longestSide = FindLongestSide(touchPanel);

            buttons.button = new Vec4[20];
            sliders.slider = new Vec4[20];

            var i = 0;
            var j = 0;
            
            // Parse out buttons and sliders in the gltf file
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name.Contains("Button"))
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;

                    float metallic = 0;
                    float roughness = 0;
                    if (item.Info.Count > 0)
                    {
                        if (item.Info.Contains("metallic"))
                        {
                            metallic = float.Parse(item.Info["metallic"]);
                        }
                        if (item.Info.Contains("roughness"))
                        {
                            roughness = float.Parse(item.Info["roughness"]);
                        }
                    }

                    buttons.button[i] = new Vec4(_positionX, _positionY, metallic, roughness);
                    buttonList.Add(new Vec4(_positionX, _positionY, metallic, roughness));
                    i++;
                }
                else if (item.Name.Contains("Slider"))
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;

                    float metallic = 0;
                    float roughness = 0;
                    if (item.Info.Count > 0)
                    {
                        if (item.Info.Contains("metallic"))
                        {
                            metallic = float.Parse(item.Info["metallic"]);
                        }
                        if (item.Info.Contains("roughness"))
                        {
                            roughness = float.Parse(item.Info["roughness"]);
                        }
                    }

                    sliders.slider[j] = new Vec4(_positionX, _positionY, metallic, roughness);
                    sliderList.Add(new Vec4(_positionX, _positionY, metallic, roughness));
                    j++;
                }
            }

            // Send the data to the shader
            touchPanelMat.SetInt("buttonAmount", i);
            touchPanelMat.SetInt("sliderAmount", j);
            touchPanelMat.SetData<ButtonData>("button", buttons);
            touchPanelMat.SetData<SliderData>("slider", sliders);

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
                if (item.Name.Contains("panel"))
                {
                    //System.Console.WriteLine("nope"); ;
                }
                else if (item.Name.Contains("Button"))
                {
                    pushButton.Button(touchPanel, item.Name, false);
                }
                else if (item.Name.Contains("Slider"))
                {
                    pushButton.Slider(touchPanel, item.Name, false);
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