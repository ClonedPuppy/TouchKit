TouchKit is a tool that allows users to design touch panel user interface (UI) interfaces within Blender, which can then be implemented in StereoKit. This tutorial provides a brief overview on how to use TouchKit.

To begin, open the UI_Template file located in the sourceFiles folder in Blender. This will display a viewport with various UI elements and a basic shape serving as the panel. Use the UI elements to create a design for the touch panel. Note that the actual images on the buttons are not important and are simply included for arrangement purposes. By default, the class is set to accept 20 buttons, 10 horizontal sliders, and 10 vertical sliders, but these values can be modified as desired.

In the Object Properties section, there are Custom Properties that hold attributes for each element that are used by TouchKit to construct the element. The panel has attributes that control the appearance of all UI elements, such as albedo, metalness, and roughness. If creating a new panel model, be sure to include these attributes.

Keep in mind that the panel model can be designed to be any desired shape, but it must meet certain requirements. The UV coordinates must span from 0 to 1 on the x axis and maintain a 1:1 aspect ratio to avoid stretched or distorted buttons. Additionally, the model and mesh names should be set to "panel" in lowercase for ease of use. It is recommended to design the touch UI from the top viewport to facilitate proper alignment of buttons and sliders.

The UI elements have the following Custom Properties to consider:

