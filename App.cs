using StereoKit;

namespace TouchMenuApp
{
    public class App
    {
        public SKSettings Settings => new SKSettings
        {
            appName = "TouchMenu",
            assetsFolder = "Assets",
            displayPreference = DisplayMode.Flatscreen
        };

        UIElements uiElements;
        TestPanel testPanel;

        Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
        Material floorMaterial;
        
        public void Init()
        {
            Renderer.SkyTex = Tex.FromCubemapEquirectangular("Container_Env.hdr", out SphericalHarmonics lighting);
            Renderer.SkyLight = lighting;
            Renderer.EnableSky = true;

            uiElements = new UIElements("Panel");
            testPanel = new TestPanel();

            floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;
        }

        public void Step()
        {
            if (SK.System.displayType == Display.Opaque)
                Default.MeshCube.Draw(floorMaterial, floorTransform);

            uiElements.DrawUI();

            testPanel.DrawTestPanel();
        }
    }
}