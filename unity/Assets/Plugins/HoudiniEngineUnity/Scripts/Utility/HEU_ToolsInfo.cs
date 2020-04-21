/*
* Copyright (c) <2018> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using UnityEngine;
using System.Collections;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;

	[System.Serializable]
	public class HEU_ToolsInfo : ScriptableObject
	{
		public float _paintBrushSize = 1f;

		public float _paintBrushOpacity = 1f;

		public int[] _paintIntValue = new int[0];

		public float[] _paintFloatValue = new float[0];

		public string[] _paintStringValue = new string[0];

		public HAPI_NodeId _lastAttributesGeoID;

		public HAPI_PartId _lastAttributesPartID;

		public string _lastAttributeNodeName;

		public string _lastAttributeName;

		public Color _brushHandleColor = new Color(0f, 0f, 0f);

		// For non-color attributes, use this color to show affected area
		public Color _affectedAreaPaintColor = new Color(1f, 0f, 0f);

		public bool _liveUpdate = true;

		public bool _isPainting = false;

		public float _editPointBoxSize = 0.1f;

		public Color _editPointBoxUnselectedColor = Color.red;

		public Color _editPointBoxSelectedColor = Color.yellow;

		public bool _recacheRequired;

		public enum PaintMergeMode
		{
			REPLACE,
			ADD,
			SUBTRACT,
			MULTIPLY
		}

		public PaintMergeMode _paintMergeMode;

		// Whether to show other geometry of the asset (instead of just the editable node's)
		public bool _showOnlyEditGeometry = true;

		// Whether to always unlock edit node, and cook its upstream input before applying attribute edits
		public bool _alwaysCookUpstream = true;
	}

}   // HoudiniEngineUnity