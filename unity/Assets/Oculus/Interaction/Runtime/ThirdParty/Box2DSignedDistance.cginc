// The MIT License
// Copyright © 2015 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
//subject to the following conditions: The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS",
// WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
// OR OTHER DEALINGS IN THE SOFTWARE.
//
// From https://www.shadertoy.com/view/4llXD7

// Signed distance to a 2D rounded box with four individual corner sizes
// p = position, b = box half width/height
// r = corner radiuses (top right, bottom right, top left, bottom left)
float sdRoundBox( in float2 p, in float2 b, in float4 r )
{
	// We choose the radius based on the quadrant we're in
    // We cap the radius based on the minimum of the box half width/height
    r.xy = (p.x>0.0)?r.xy : r.zw;
    r.x = (p.y>0.0)?r.x : r.y;
    r.x = min(2.0f*r.x, min(b.x, b.y));

    float2 q = abs(p)-b+r.x;
    return min(max(q.x,q.y),0.0) + length(max(q,0.0)) - r.x;
}
