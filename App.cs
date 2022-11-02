using StereoKit;

namespace TouchMenuApp
{
    public class App
    {
        public SKSettings Settings => new SKSettings
        {
            appName = "TouchMenu",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.MixedReality
        };

        UIElements uiElements;

        UIElements uiPhone;
        
        Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
        Material floorMaterial;
        
        public void Init()
        {
            Renderer.SkyTex = Tex.FromCubemapEquirectangular("Courtyard.hdr", out SphericalHarmonics lighting);
            Renderer.SkyLight = lighting;
            Renderer.EnableSky = true;

            uiElements = new UIElements("Panel");

            //uiPhone = new UIElements("Phone");

            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            if (SK.ActiveDisplayMode == DisplayMode.Flatscreen)
            {
                //Renderer.CameraRoot = Matrix.TR(V.XYZ(0, 0.5f, 0.185f), Quat.FromAngles(-69, 0, 0));
            }
        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            uiElements.DrawUI();

            //uiPhone.DrawUI();

            //if (uiElements.buttonStates.Count > 0)
            //{
            //    System.Console.WriteLine(uiElements.buttonStates["Y Value"]);
            //}
        }
    }
}