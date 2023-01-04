#### TouchKit is a tool that allows users to design touch panel user interface (UI) interfaces within Blender, which can then be implemented in StereoKit. This tutorial provides a brief overview on how to use TouchKit.

To begin, open the UI_Template file located in the sourceFiles folder in Blender. This will display a viewport with various UI elements and a basic shape serving as the panel. Use the UI elements to create a design for the touch panel. Note that the actual images on the buttons are not important and are simply included for arrangement purposes. By default, the class is set to accept 20 buttons, 10 horizontal sliders, and 10 vertical sliders, but these values can be modified as desired.

In the Object Properties section, there are Custom Properties that hold attributes for each element that are used by TouchKit to construct the element. The panel has attributes that control the appearance of all UI elements, such as albedo, metalness, and roughness. If creating a new panel model, be sure to include these attributes as well as a Material with a Principled BRDF Shader.

Keep in mind that the panel model can be designed to be any desired shape, but it must meet certain requirements. The UV coordinates must span from 0 to 1 on the U axis and maintain a 1:1 aspect ratio to avoid stretched or distorted buttons. Additionally, the model and mesh names should be set to "panel" in lowercase for ease of use. It is recommended to design the touch UI from the top viewport to facilitate proper alignment of buttons and sliders.

The UI elements have the following Custom Properties:

Buttons:

- defState: Default state of the button (0/1)
- label: Unique name of the button
- type: Type of button (button/toggle)

Sliders:

- defState: Default state of the slider (0-1)
- label: Unique name of the slider
- type: Type of slider (hslider / vslider)

It is important to assign a unique name to all UI elements as this name is used to retrieve the state of the element.

To finalize the touch panel design, use the Export function in Blender to export the scene as a .glb file. The exporter in the UI_Template file is already configured with the appropriate settings, including the option to only export renderable items. This ensures that reference images on the buttons are not included in the exported file.

Once the .glb file has been generated, move it to the /Asset folder and update the panel name in the constructor to the name of the exported .glb file (excluding the .glb extension).

After completing these steps, build the project to test out the new touch panel.