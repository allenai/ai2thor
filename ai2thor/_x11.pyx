import numpy as np
cdef extern from "sys/ipc.h":
    ctypedef struct key_t:
        pass
    key_t IPC_PRIVATE
    int IPC_CREAT
    int IPC_RMID

cdef extern from "sys/shm.h":
    int shmget(key_t __key, size_t __size, int __shmflg)
    void *shmat(int __shmid, const void *__shmaddr, int __shmflg)
    int shmdt (const void *__shmaddr)
    ctypedef struct shmid_ds:
        pass
    int shmctl(int shmid, int cmd, shmid_ds *buf)

cdef extern from "X11/Xutil.h":
    pass


DEF XNone = "None"
DEF CurrentTime = "None"

ctypedef unsigned long CARD32
ctypedef unsigned short CARD16
ctypedef unsigned char CARD8

cdef extern from "X11/X.h":
    unsigned long NoSymbol
    int CWX
    int CWY



cdef extern from "X11/Xlib.h":
    ctypedef struct Display:
        pass
    # To make it easier to translate stuff in the X header files into
    # appropriate pyrex declarations, without having to untangle the typedefs
    # over and over again, here are some convenience typedefs.  (Yes, CARD32
    # really is 64 bits on 64-bit systems.  Why? Because CARD32 was defined
    # as a long.. and a long is now 64-bit, it was easier to do this than
    # to change a lot of existing X11 client code)
    ctypedef CARD32 XID

    ctypedef int Bool
    ctypedef int Status
    ctypedef CARD32 Atom
    ctypedef XID Drawable
    ctypedef XID Window
    ctypedef XID Pixmap

    ctypedef CARD32 VisualID

    ctypedef struct Visual:
        void    *ext_data       #XExtData *ext_data;     /* hook for extension to hang data */
        VisualID visualid
        int c_class
        unsigned long red_mask
        unsigned long green_mask
        unsigned long blue_mask
        int bits_per_rgb
        int map_entries


    int XFree(void * data)

    Bool XQueryExtension(Display * display, char *name,
                         int *major_opcode_return, int *first_event_return, int *first_error_return)


    ctypedef struct XWindowAttributes:
        int x, y, width, height, depth, border_width
        Visual *visual

    Status XGetWindowAttributes(Display * display, Window w,
                                XWindowAttributes * attributes)

    ctypedef char* XPointer

    ctypedef struct XImage:
        int width
        int height
        int xoffset             # number of pixels offset in X direction
        int format              # XYBitmap, XYPixmap, ZPixmap
        char *data              # pointer to image data
        int byte_order          # data byte order, LSBFirst, MSBFirst
        int bitmap_unit         # quant. of scanline 8, 16, 32
        int bitmap_bit_order    # LSBFirst, MSBFirst
        int bitmap_pad          # 8, 16, 32 either XY or ZPixmap
        int depth               # depth of image
        int bytes_per_line      # accelerator t next scanline
        int bits_per_pixel      # bits per pixel (ZPixmap)
        unsigned long red_mask  # bits in z arrangement
        unsigned long green_mask
        unsigned long blue_mask
        XPointer *obdata
        void *funcs
    
    ctypedef struct XWindowChanges:
        int x
        int y
        int width
        int height
        int border_width
        Window sibling
        int stack_mode


    unsigned long AllPlanes
    int XYPixmap
    int ZPixmap
    int MSBFirst
    int LSBFirst

    XImage *XGetImage(Display *display, Drawable d,
            int x, int y, unsigned int width, unsigned int  height,
            unsigned long plane_mask, int format)

    void XDestroyImage(XImage *ximage)

    Status XGetGeometry(Display *display, Drawable d, Window *root_return,
                        int *x_return, int *y_return, unsigned int  *width_return, unsigned int *height_return,
                        unsigned int *border_width_return, unsigned int *depth_return)

    int XConfigureWindow(Display *display, Window w, unsigned int value_mask, XWindowChanges *changes);
    int XMoveWindow(Display *display, Window w, int x, int y);
    int XFlush(Display *display);


    Display *XOpenDisplay(char * name)

    int XSync(Display *display, Bool discard)

    Visual *DefaultVisual(Display *display, int screen_number)

    int XCloseDisplay(Display * xdisplay)

    Status XQueryTree(Display * display, Window w,
                      Window * root, Window * parent,
                      Window ** children, unsigned int * nchildren)

    Window XDefaultRootWindow(Display *display)

cdef extern from "X11/extensions/Xcomposite.h":
    Bool XCompositeQueryExtension(Display *, int *, int *)
    Status XCompositeQueryVersion(Display *, int * major, int * minor)

cdef extern from "X11/extensions/XShm.h":
    unsigned int ShmCompletion
    ctypedef struct ShmSeg:
        pass
    ctypedef struct XShmSegmentInfo:
        ShmSeg shmseg   # resource id
        int shmid       # kernel id
        void *shmaddr   # address in client
        Bool readOnly   # how the server should attach it

    XShmQueryExtension(Display *display)
    Bool XShmQueryVersion(Display *display, int *major, int *minor, Bool *pixmaps)

    Bool XShmAttach(Display *display, XShmSegmentInfo *shminfo)
    Bool XShmDetach(Display *display, XShmSegmentInfo *shminfo)

    XImage *XShmCreateImage(Display *display, Visual *visual,
                            unsigned int depth, int format, char *data,
                            XShmSegmentInfo *shminfo,
                            unsigned int width, unsigned int height)

    Bool XShmGetImage(Display *display, Drawable d, XImage *image,
                      int x, int y,
                      unsigned long plane_mask)

    int XShmGetEventBase(Display *display)



SBFirst = {
           MSBFirst : "MSBFirst",
           LSBFirst : "LSBFirst"
           }




cdef class X11:
    cdef Display *display
    cdef XShmSegmentInfo shminfo
    cdef XImage *image
    cdef bint freed


    def __cinit__(self):
        self.shminfo.shmaddr = <char *> -1
        self.display = XOpenDisplay(NULL)
        if self.display == NULL:
            self.free()
            raise Exception("XOpenDisplay error -  display could not be opened")

    def initialize(self, int width, int height):
        cdef size_t size
        cdef int depth = 32

        # in case we are reinitializing
        self.free_shm()

        self.image = XShmCreateImage(self.display, DefaultVisual(self.display, 0), depth,
                          ZPixmap, NULL, &self.shminfo,
                          width, height)

        if self.image == NULL:
            self.free()
            raise Exception("Failed to create XImage")

        size = self.image.bytes_per_line * (self.image.height)
        self.shminfo.shmid = shmget(IPC_PRIVATE, size, IPC_CREAT | 0777)

        if self.shminfo.shmid == -1:
            self.free()
            raise Exception("failed to allocate shared memory ")

        self.image.data = <char *>shmat(self.shminfo.shmid, NULL, 0)
        self.shminfo.shmaddr = self.image.data
        if self.shminfo.shmaddr == <char *> -1:
            self.free()
            raise Exception("failed to allocate shared memory ")
        # set as read/write, and attach to the display:
        self.shminfo.readOnly = False
        a = XShmAttach(self.display, &self.shminfo)
        XSync(self.display, False );
        if not a:
            self.free()
            raise Exception("Failed to attach to shared memory")

    def get_xshm_image(self, long window_id):
        cdef int width = self.image.width
        cdef int height = self.image.height

        if not XShmGetImage(self.display, window_id, self.image, 0, 0, AllPlanes):
            raise Exception("XShmGetImage failed for window_id %s" % window_id)

        cdef char[:] mview = <char[:(height * width * 4)]>self.image.data

        # The data field comes in as format (4 bytes) BGRN, so this
        # slicing reverses the order and removes the null field so 
        # that our output matches that of Unity (RGB)
        return np.frombuffer(mview, dtype=np.uint8).reshape(height, width, 4)[...,2::-1]
    
    def move_window(self, int window_id, int x, int y):
        res = XMoveWindow(self.display, window_id, x, y)
        # XXX check res
        res = XFlush(self.display)

    def get_root_geometry(self):
        cdef Window window = XDefaultRootWindow(self.display)
        return self.get_geometry(window)

    #def has_composite(self):
    #    cdef int major, minor 
    #    return XCompositeQueryExtension(self.display, &major, &minor) == 1

    def get_geometry(self, window_id):
        cdef Window root
        cdef int x, y
        cdef unsigned int width, height, border_width, depth
        if not XGetGeometry(self.display, window_id, &root,
                        &x, &y, &width, &height, &border_width, &depth):
            return 0

        return (width, height, x, y)

    def parent_window(self, child_window):
        cdef Window root = 0, parent = 0
        cdef Window * children = <Window *> 0
        cdef unsigned int nchildren = 0

        if not XQueryTree(self.display,
                          child_window,
                          &root, &parent, &children, &nchildren):
            return None

        if parent == XDefaultRootWindow(self.display):
            return None

        return parent

    def list_all_windows(self, root_window = 0):
        cdef Window root = 0, parent = 0
        cdef Window * children = <Window *> 0
        cdef unsigned int nchildren = 0
        cdef object pychildren
        cdef object pyparent
        if root_window == 0:
            root_window = XDefaultRootWindow(self.display)
        if not XQueryTree(self.display,
                          root_window,
                          &root, &parent, &children, &nchildren):
            return (None, [])

        pychildren = []

        for i from 0 <= i < nchildren:
            #we cannot get the gdk window for wid=0
            if children[i]>0:
                pychildren.append(children[i])

        return pychildren


    def __dealloc__(self):
        self.cleanup()

    def cleanup(self):
        self.free()

    cdef free_shm(self):
        has_shm = self.shminfo.shmaddr!=<char *> -1
        if has_shm:
            XShmDetach(self.display, &self.shminfo)

        if self.image != NULL:
            XDestroyImage(self.image)
            self.image = NULL

        if has_shm:
            shmctl(self.shminfo.shmid, IPC_RMID, NULL)
            shmdt(self.shminfo.shmaddr)
            self.shminfo.shmaddr = <char *> -1
            self.shminfo.shmid = -1

    cdef free(self):
        self.free_shm()
        if self.display != NULL:
            XCloseDisplay(self.display)
            self.display = NULL
