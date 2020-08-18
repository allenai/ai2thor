class Camera:
    def __init__(
            self,
            fov: float = 60,
            width: int = 256,
            height: int = 256,
            render_frame: bool = True,
            render_depth: bool = False,
            render_class_segmentation: bool = False,
            render_instance_segmentation: bool = False,
            frame_as_bgr: bool = False):
        self.fov = fov
        self.width = width
        self.height = height
        self.render_frame = render_frame
        self.render_depth = render_depth
        self.render_class_segmentation = render_class_segmentation
        self.render_instance_segmentation = render_instance_segmentation
        self.frame_as_bgr = frame_as_bgr

    @staticmethod
    def _format_field(field, val, spaces=4):
        return spaces * ' ' + f'{field}: {val}\n'

    def __repr__(self):
        f = Camera._format_field
        out = '{\n'
        out += f('fov', self.fov)
        out += f('width', self.width)
        out += f('height', self.height)
        out += f('render_frame', self.render_frame)
        out += f('render_depth', self.render_depth)
        out += f('render_class_segmentation', self.render_class_segmentation)
        out += f('render_instance_segmentation',
                 self.render_instance_segmentation)
        out += f('frame_as_bgr', self.frame_as_bgr)
        out += '}'
        return out

    def __str__(self):
        return self.__repr__()
