/************************************************************************************
Filename    :   ONSPPropagationMaterial.cs
Content     :   Propagation material class
                Attach to geometry to define material properties
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Spatializer.Propagation;

public sealed class ONSPPropagationMaterial : MonoBehaviour
{
	public enum Preset
	{
		Custom,
		AcousticTile,
		Brick,
		BrickPainted,
		Carpet,
		CarpetHeavy,
		CarpetHeavyPadded,
		CeramicTile,
		Concrete,
		ConcreteRough,
		ConcreteBlock,
		ConcreteBlockPainted,
		Curtain,
		Foliage,
		Glass,
		GlassHeavy,
		Grass,
		Gravel,
		GypsumBoard,
		PlasterOnBrick,
		PlasterOnConcreteBlock,
		Soil,
		SoundProof,
		Snow,
		Steel,
		Water,
		WoodThin,
		WoodThick,
		WoodFloor,
		WoodOnConcrete
	}

	[Serializable]
	public sealed class Point
	{
		public float frequency;
		public float data;

		public Point( float frequency = 0, float data = 0 )
		{
			this.frequency = frequency;
			this.data = data;
		}
		
		public static implicit operator Point(Vector2 v)
		{
			return new Point(v.x, v.y);
		}
		
		public static implicit operator Vector2(Point point)
		{
			return new Vector2(point.frequency, point.data);
		}
	}

	[Serializable]
	public sealed class Spectrum
	{
		public int selection = int.MaxValue;
		public List<Point> points = new List<Point>();

		public float this[float f]
		{

			get
			{

				if (points.Count > 0)
				{

					Point lower = new Point(float.MinValue);
					Point upper = new Point(float.MaxValue);

					foreach (Point point in points)
					{
						if (point.frequency < f)
						{
							if (point.frequency > lower.frequency)
								lower = point;
						}
						else
						{
							if (point.frequency < upper.frequency)
								upper = point;
						}
					}

					if (lower.frequency == float.MinValue)
						lower.data = points.OrderBy(p => p.frequency).First().data;
					if (upper.frequency == float.MaxValue)
						upper.data = points.OrderBy(p => p.frequency).Last().data;

					return lower.data + (f - lower.frequency) *
					  (upper.data - lower.data) / (upper.frequency - lower.frequency);

				}

				return 0f;

			}
		}
	}
	
	//***********************************************************************
	// Private Fields
	
	public IntPtr materialHandle = IntPtr.Zero;
	
	//***********************************************************************
	// Public Fields
	
	[Tooltip("Absorption")]
	public Spectrum absorption   = new Spectrum();
    [Tooltip("Transmission")]
    public Spectrum transmission = new Spectrum();
    [Tooltip("Scattering")]
    public Spectrum scattering   = new Spectrum();

    [SerializeField]
	private Preset preset_ = Preset.Custom;
	public Preset preset
	{
		get { return preset_; }
		set
		{
			this.SetPreset( value );
			preset_ = value;
		}
	}
	
	//***********************************************************************
	// Start / Destroy
	
	/// Initialize the audio material. This is called after Awake() and before the first Update().
	void Start()
	{
		StartInternal();
	}

	public void StartInternal()
	{
		// Ensure that the material is not initialized twice.
		if ( materialHandle != IntPtr.Zero )
			return;
						
		// Create the internal material.
		if (ONSPPropagation.Interface.CreateAudioMaterial( out materialHandle ) != ONSPPropagationGeometry.OSPSuccess)
			throw new Exception("Unable to create internal audio material");
		
		// Run the updates to initialize the material.
		UploadMaterial();
	}
	
	/// Destroy the audio scene. This is called when the scene is deleted.
	void OnDestroy()
	{
		DestroyInternal();
	}

	public void DestroyInternal()
	{
		if ( materialHandle != IntPtr.Zero )
		{
            // Destroy the material.
            ONSPPropagation.Interface.DestroyAudioMaterial(materialHandle);
			materialHandle = IntPtr.Zero;
		}
	}
	
	//***********************************************************************
	// Upload
	
	public void UploadMaterial()
	{
		if ( materialHandle == IntPtr.Zero )
			return;

        // Absorption
        ONSPPropagation.Interface.AudioMaterialReset(materialHandle, MaterialProperty.ABSORPTION);

		foreach ( Point p in absorption.points )
            ONSPPropagation.Interface.AudioMaterialSetFrequency(materialHandle, MaterialProperty.ABSORPTION, 
                                                          p.frequency, p.data );

        // Transmission
        ONSPPropagation.Interface.AudioMaterialReset(materialHandle, MaterialProperty.TRANSMISSION);

        foreach (Point p in transmission.points)
            ONSPPropagation.Interface.AudioMaterialSetFrequency(materialHandle, MaterialProperty.TRANSMISSION, 
                                                          p.frequency, p.data);

        // Scattering
        ONSPPropagation.Interface.AudioMaterialReset(materialHandle, MaterialProperty.SCATTERING);

        foreach (Point p in scattering.points)
            ONSPPropagation.Interface.AudioMaterialSetFrequency(materialHandle, MaterialProperty.SCATTERING,
                                                          p.frequency, p.data);

    }

    //***********************************************************************

    public void SetPreset(Preset preset )
	{
        ONSPPropagationMaterial material = this;

		switch ( preset )
		{
			case Preset.AcousticTile:			AcousticTile(ref material);				break;
			case Preset.Brick:					Brick(ref material);					break;
			case Preset.BrickPainted:			BrickPainted(ref material);				break;
			case Preset.Carpet:					Carpet(ref material);					break;
			case Preset.CarpetHeavy:			CarpetHeavy(ref material);				break;
			case Preset.CarpetHeavyPadded:		CarpetHeavyPadded(ref material);		break;
			case Preset.CeramicTile:			CeramicTile(ref material);				break;
			case Preset.Concrete:				Concrete(ref material);					break;
			case Preset.ConcreteRough:			ConcreteRough(ref material);			break;
			case Preset.ConcreteBlock:			ConcreteBlock(ref material);			break;
			case Preset.ConcreteBlockPainted:	ConcreteBlockPainted(ref material);		break;
			case Preset.Curtain:				Curtain(ref material);					break;
			case Preset.Foliage:				Foliage(ref material);					break;
			case Preset.Glass:					Glass(ref material);					break;
			case Preset.GlassHeavy:				GlassHeavy(ref material);				break;
			case Preset.Grass:					Grass(ref material);					break;
			case Preset.Gravel:					Gravel(ref material);					break;
			case Preset.GypsumBoard:			GypsumBoard(ref material);				break;
			case Preset.PlasterOnBrick:			PlasterOnBrick(ref material);			break;
			case Preset.PlasterOnConcreteBlock:	PlasterOnConcreteBlock(ref material);	break;
			case Preset.Soil:					Soil(ref material);						break;
			case Preset.SoundProof:				SoundProof(ref material);				break;
			case Preset.Snow:					Snow(ref material);						break;
			case Preset.Steel:					Steel(ref material);					break;
			case Preset.Water:					Water(ref material);					break;
			case Preset.WoodThin:				WoodThin(ref material);					break;
			case Preset.WoodThick:				WoodThick(ref material);				break;
			case Preset.WoodFloor:				WoodFloor(ref material);				break;
			case Preset.WoodOnConcrete:			WoodOnConcrete(ref material);			break;
            case Preset.Custom:
                break;
            default:
				break;
		}
	}
	
	//***********************************************************************
	
	private static void AcousticTile(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .50f), new Point(250f, .70f), new Point(500f, .60f), new Point(1000f, .70f), new Point(2000f, .70f), new Point(4000f, .50f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .15f), new Point(500f, .20f), new Point(1000f, .20f), new Point(2000f, .25f), new Point(4000f, .30f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .05f), new Point(250f, .04f), new Point(500f, .03f), new Point(1000f, .02f), new Point(2000f, .005f), new Point(4000f, .002f) };
	}

	private static void Brick(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .02f), new Point(250f, .02f), new Point(500f, .03f), new Point(1000f, .04f), new Point(2000f, .05f), new Point(4000f, .07f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .25f), new Point(500f, .30f), new Point(1000f, .35f), new Point(2000f, .40f), new Point(4000f, .45f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .025f), new Point(250f, .019f), new Point(500f, .01f), new Point(1000f, .0045f), new Point(2000f, .0018f), new Point(4000f, .00089f) };
	}

	private static void BrickPainted(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .01f),  new Point(500f, .02f), new Point(1000f, .02f), new Point(2000f, .02f), new Point(4000f, .03f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .15f), new Point(250f, .15f), new Point(500f, .20f), new Point(1000f, .20f), new Point(2000f, .20f), new Point(4000f, .25f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .025f), new Point(250f, .019f), new Point(500f, .01f), new Point(1000f, .0045f), new Point(2000f, .0018f), new Point(4000f, .00089f) };
	}

	private static void Carpet(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .05f), new Point(500f, .10f), new Point(1000f, .20f), new Point(2000f, .45f), new Point(4000f, .65f) };
		
		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .15f), new Point(1000f, .20f), new Point(2000f, .30f), new Point(4000f, .45f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void CarpetHeavy(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .02f), new Point(250f, .06f), new Point(500f, .14f), new Point(1000f, .37f), new Point(2000f, .48f), new Point(4000f, .63f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .15f), new Point(500f, .20f), new Point(1000f, .25f), new Point(2000f, .35f),  new Point(4000f, .50f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void CarpetHeavyPadded(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .08f), new Point(250f, .24f), new Point(500f, .57f), new Point(1000f, .69f), new Point(2000f, .71f), new Point(4000f, .73f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .15f), new Point(500f, .20f), new Point(1000f, .25f), new Point(2000f, .35f), new Point(4000f, .50f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void CeramicTile(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .01f), new Point(500f, .01f), new Point(1000f, .01f), new Point(2000f, .02f), new Point(4000f, .02f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .12f), new Point(500f, .14f), new Point(1000f, .16f), new Point(2000f, .18f), new Point(4000f, .20f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void Concrete(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .01f), new Point(500f, .02f), new Point(1000f, .02f), new Point(2000f, .02f), new Point(4000f, .02f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .11f), new Point(500f, .12f), new Point(1000f, .13f), new Point(2000f, .14f), new Point(4000f, .15f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void ConcreteRough(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .02f), new Point(500f, .04f), new Point(1000f, .06f), new Point(2000f, .08f), new Point(4000f, .10f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .12f), new Point(500f, .15f), new Point(1000f, .20f), new Point(2000f, .25f), new Point(4000f, .30f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}

	private static void ConcreteBlock(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .36f), new Point(250f, .44f), new Point(500f, .31f), new Point(1000f, .29f), new Point(2000f, .39f), new Point(4000f, .21f) };
			
		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .12f), new Point(500f, .15f), new Point(1000f, .20f), new Point(2000f, .30f), new Point(4000f, .40f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .02f), new Point(250f, .01f), new Point(500f, .0063f), new Point(1000f, .0035f), new Point(2000f, .00011f), new Point(4000f, .00063f) };
	}

	private static void ConcreteBlockPainted(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .05f), new Point(500f, .06f), new Point(1000f, .07f), new Point(2000f, .09f), new Point(4000f, .08f) };
		
		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .11f), new Point(500f, .13f), new Point(1000f, .15f), new Point(2000f, .16f), new Point(4000f, .20f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .02f), new Point(250f, .01f), new Point(500f, .0063f), new Point(1000f, .0035f), new Point(2000f, .00011f), new Point(4000f, .00063f) };
	}

	private static void Curtain(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .07f), new Point(250f, .31f), new Point(500f, .49f), new Point(1000f, .75f), new Point(2000f, .70f), new Point(4000f, .60f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .15f), new Point(500f, .2f), new Point(1000f, .3f), new Point(2000f, .4f), new Point(4000f, .5f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .42f), new Point(250f, .39f), new Point(500f, .21f), new Point(1000f, .14f), new Point(2000f, .079f), new Point(4000f, .045f) };
	}

	private static void Foliage(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .03f), new Point(250f, .06f), new Point(500f, .11f), new Point(1000f, .17f), new Point(2000f, .27f), new Point(4000f, .31f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .3f), new Point(500f, .4f), new Point(1000f, .5f), new Point(2000f, .7f), new Point(4000f, .8f) };
		
		material.transmission.points = new List<Point>(){
			new Point(125f, .9f), new Point(250f, .9f), new Point(500f, .9f), new Point(1000f, .8f), new Point(2000f, .5f), new Point(4000f, .3f) };
	}
	
	private static void Glass(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .35f), new Point(250f, .25f), new Point(500f, .18f), new Point(1000f, .12f), new Point(2000f, .07f), new Point(4000f, .05f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .05f), new Point(250f, .05f), new Point(500f, .05f), new Point(1000f, .05f), new Point(2000f, .05f), new Point(4000f, .05f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .125f), new Point(250f, .089f), new Point(500f, .05f), new Point(1000f, .028f), new Point(2000f, .022f), new Point(4000f, .079f) };
	}

	private static void GlassHeavy(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .18f),  new Point(250f, .06f), new Point(500f, .04f),  new Point(1000f, .03f), new Point(2000f, .02f), new Point(4000f, .02f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .05f), new Point(250f, .05f), new Point(500f, .05f), new Point(1000f, .05f), new Point(2000f, .05f), new Point(4000f, .05f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .056f), new Point(250f, .039f), new Point(500f, .028f), new Point(1000f, .02f), new Point(2000f, .032f), new Point(4000f, .014f) };
	}

	private static void Grass(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .11f), new Point(250f, .26f), new Point(500f, .60f), new Point(1000f, .69f), new Point(2000f, .92f), new Point(4000f, .99f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .30f), new Point(250f, .30f), new Point(500f, .40f), new Point(1000f, .50f), new Point(2000f, .60f), new Point(4000f, .70f) };

		material.transmission.points = new List<Point>();
	}

	private static void Gravel(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .25f), new Point(250f, .60f), new Point(500f, .65f), new Point(1000f, .70f), new Point(2000f, .75f), new Point(4000f, .80f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .30f), new Point(500f, .40f), new Point(1000f, .50f), new Point(2000f, .60f), new Point(4000f, .70f) };

		material.transmission.points = new List<Point>();
	}

	private static void GypsumBoard(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .29f), new Point(250f, .10f), new Point(500f, .05f), new Point(1000f, .04f), new Point(2000f, .07f), new Point(4000f, .09f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .11f), new Point(500f, .12f), new Point(1000f, .13f), new Point(2000f, .14f), new Point(4000f, .15f) };
			
		material.transmission.points = new List<Point>(){
			new Point(125f, .035f), new Point(250f, .0125f), new Point(500f, .0056f), new Point(1000f, .0025f), new Point(2000f, .0013f), new Point(4000f, .0032f) };
	}

	private static void PlasterOnBrick(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .02f), new Point(500f, .02f), new Point(1000f, .03f), new Point(2000f, .04f), new Point(4000f, .05f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .25f), new Point(500f, .30f), new Point(1000f, .35f), new Point(2000f, .40f), new Point(4000f, .45f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .025f), new Point(250f, .019f), new Point(500f, .01f), new Point(1000f, .0045f), new Point(2000f, .0018f), new Point(4000f, .00089f) };
	}

	private static void PlasterOnConcreteBlock(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .12f), new Point(250f, .09f), new Point(500f, .07f), new Point(1000f, .05f), new Point(2000f, .05f), new Point(4000f, .04f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .25f), new Point(500f, .30f), new Point(1000f, .35f), new Point(2000f, .40f),  new Point(4000f, .45f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .02f), new Point(250f, .01f), new Point(500f, .0063f), new Point(1000f, .0035f), new Point(2000f, .00011f), new Point(4000f, .00063f) };
	}

	private static void Soil(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .15f), new Point(250f, .25f), new Point(500f, .40f), new Point(1000f, .55f), new Point(2000f, .60f), new Point(4000f, .60f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .20f), new Point(500f, .25f), new Point(1000f, .40f), new Point(2000f, .55f), new Point(4000f, .70f) };

		material.transmission.points = new List<Point>();
	}

	private static void SoundProof(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{ new Point(1000f, 1.0f) };
		material.scattering.points = new List<Point>{ new Point(1000f, 0.0f) };
		material.transmission.points = new List<Point>();
	}

	private static void Snow(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .45f), new Point(250f, .75f), new Point(500f, .90f), new Point(1000f, .95f), new Point(2000f, .95f), new Point(4000f, .95f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .20f), new Point(250f, .30f), new Point(500f, .40f), new Point(1000f, .50f), new Point(2000f, .60f), new Point(4000f, .75f) };

		material.transmission.points = new List<Point>();
	}

	private static void Steel(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .05f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .07f), new Point(4000f, .02f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .10f), new Point(4000f, .10f) };
			
		material.transmission.points = new List<Point>(){
			new Point(125f, .25f), new Point(250f, .2f), new Point(500f, .17f), new Point(1000f, .089f), new Point(2000f, .089f), new Point(4000f, .0056f) };
	}

	private static void Water(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .01f), new Point(250f, .01f), new Point(500f, .01f), new Point(1000f, .02f), new Point(2000f, .02f), new Point(4000f, .03f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .07f), new Point(2000f, .05f), new Point(4000f, .05f) };
			
		material.transmission.points = new List<Point>(){
			new Point(125f, .03f), new Point(250f, .03f), new Point(500f, .03f), new Point(1000f, .02f), new Point(2000f, .015f), new Point(4000f, .01f) };
	}

	private static void WoodThin(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .42f), new Point(250f, .21f), new Point(500f, .10f), new Point(1000f, .08f), new Point(2000f, .06f), new Point(4000f, .06f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .10f), new Point(4000f, .15f) };
			
		material.transmission.points = new List<Point>(){
			new Point(125f, .2f), new Point(250f, .125f), new Point(500f, .079f), new Point(1000f, .1f), new Point(2000f, .089f), new Point(4000f, .05f) };
	}

	private static void WoodThick(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .19f), new Point(250f, .14f), new Point(500f, .09f), new Point(1000f, .06f), new Point(2000f, .06f), new Point(4000f, .05f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .10f), new Point(4000f, .15f) };
			
		material.transmission.points = new List<Point>(){
			new Point(125f, .035f), new Point(250f, .028f), new Point(500f, .028f), new Point(1000f, .028f), new Point(2000f, .011f), new Point(4000f, .0071f) };
	}

	private static void WoodFloor(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .15f), new Point(250f, .11f), new Point(500f, .10f), new Point(1000f, .07f), new Point(2000f, .06f), new Point(4000f, .07f) };

		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .10f), new Point(4000f, .15f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .071f), new Point(250f, .025f), new Point(500f, .0158f), new Point(1000f, .0056f), new Point(2000f, .0035f), new Point(4000f, .0016f) };
	}

	private static void WoodOnConcrete(ref ONSPPropagationMaterial material)
	{
		material.absorption.points = new List<Point>{
			new Point(125f, .04f),  new Point(250f, .04f), new Point(500f, .07f), new Point(1000f, .06f),  new Point(2000f, .06f), new Point(4000f, .07f) };
		
		material.scattering.points = new List<Point>{
			new Point(125f, .10f), new Point(250f, .10f), new Point(500f, .10f), new Point(1000f, .10f), new Point(2000f, .10f), new Point(4000f, .15f) };

		material.transmission.points = new List<Point>(){
			new Point(125f, .004f), new Point(250f, .0079f), new Point(500f, .0056f), new Point(1000f, .0016f), new Point(2000f, .0014f), new Point(4000f, .0005f) };
	}
}
