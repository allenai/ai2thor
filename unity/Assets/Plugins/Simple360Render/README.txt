= 360° Screenshot Capture =

Online documentation & example code available at: https://github.com/yasirkula/Unity360ScreenshotCapture
E-mail: yasirkula@gmail.com

1. ABOUT
This plugin helps you capture 360° screenshots in equirectangular format during gameplay.

2. HOW TO
Simply call the following function:

public static byte[] I360Render.Capture( int width = 1024, bool encodeAsJPEG = true, Camera renderCam = null, bool faceCameraDirection = true );

- width: The width of the resulting image. It must be a power of 2. The height will be equal to width / 2. Be aware that maximum allowed image width is 8192 pixels
- encodeAsJPEG: determines whether the image will be encoded as JPEG or PNG
- renderCam: the camera that will be used to render the 360° image. If set to null, Camera.main will be used
- faceCameraDirection: if set to true, when the 360° image is viewed in a 360° viewer, initial camera rotation will match the rotation of the renderCam. Otherwise, initial camera rotation will be Quaternion.identity (facing Z+ axis)

This function returns a byte[] object that you can write to a file using File.WriteAllBytes.