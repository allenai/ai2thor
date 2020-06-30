using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MCSPerformerManager : AgentManager {
    public List<byte[]> imageList = new List<byte[]>();
    public List<byte[]> imageClassList = new List<byte[]>();
    public List<byte[]> imageDepthList = new List<byte[]>();
    public List<byte[]> imageFlowList = new List<byte[]>();
    public List<byte[]> imageNormalList = new List<byte[]>();
    public List<byte[]> imageObjectList = new List<byte[]>();

    private int imageCount = 0;

    private WWWForm AddImageDataToForm(WWWForm form, List<byte[]> list, string field) {
        list.ForEach((image) => {
            if (image != null) {
                form.AddBinaryData(field, image);
            }
        });
        return form;
    }

    public void ClearSavedImages() {
        this.imageCount = 0;
        this.imageList.Clear();
        this.imageClassList.Clear();
        this.imageDepthList.Clear();
        this.imageFlowList.Clear();
        this.imageNormalList.Clear();
        this.imageObjectList.Clear();
    }

    public override IEnumerator EmitFrame() {
        // Set renderImage to false before calling AgentManager.EmitFrame because we've rendered all the images already
        // and we don't want AgentManager.EmitFrame to re-render the images.
        this.renderImage = false;
        return base.EmitFrame();
    }

    public void FinalizeEmit() {
        // This will notify the AgentManager to send the action output metadata and our saved images to the Python API.
        base.setReadyToEmit(true);
    }

    protected override MultiAgentMetadata FinalizeMultiAgentMetadata(MultiAgentMetadata metadata) {
        // Assumption: The existing metadata for any MCS use case will always have exactly one agent.
        List<MetadataWrapper> metadataList = metadata.agents.ToList();
        // Hack: The AI2-THOR Python API will only accept multiple images if multiple agents exist in the metadata, so
        // pretend we have separate agents for each image that we want to send (just copy the existing agent object).
        for (int i = 0; i < (this.imageCount - 1); ++i) {
            metadataList.Add(metadata.agents[0]);
        }
        metadata.agents = metadataList.ToArray();
        return metadata;
    }

    protected override WWWForm InitializeForm(WWWForm form) {
        WWWForm formToReturn = base.InitializeForm(form);
        // Add our saved images to the form that is sent to the Python API.
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageList, "image");
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageClassList, "image_classes");
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageDepthList, "image_depth");
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageFlowList, "image_flow");
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageNormalList, "image_normals");
        formToReturn = this.AddImageDataToForm(formToReturn, this.imageObjectList, "image_ids");
        return formToReturn;
    }

    public void SaveImages(ImageSynthesis imageSynthesis) {
        if (this.renderImage) {
            byte[] image = this.captureScreen();
            this.imageList.Add(image);
        }
        this.SaveImageForActionOutput(imageSynthesis, this.renderClassImage, "_class", this.imageClassList);
        this.SaveImageForActionOutput(imageSynthesis, this.renderDepthImage, "_depth", this.imageDepthList);
        this.SaveImageForActionOutput(imageSynthesis, this.renderFlowImage, "_flow", this.imageFlowList);
        this.SaveImageForActionOutput(imageSynthesis, this.renderNormalsImage, "_normals", this.imageNormalList);
        this.SaveImageForActionOutput(imageSynthesis, this.renderObjectImage, "_id", this.imageObjectList);
        this.imageCount++;
    }

    private void SaveImageForActionOutput(ImageSynthesis imageSynthesis, bool flag, string type, List<byte[]> list) {
        if (flag && imageSynthesis.hasCapturePass(type)) {
            byte[] image = imageSynthesis.Encode(type);
            list.Add(image);
        }
    }

	public override void setReadyToEmit(bool readyToEmit) {
        // Don't set readyToEmit yet because we don't want the AgentManager to send the action output metadata and our
        // saved images to the Python API until AFTER we've rendered and saved all the images (and call FinalizeEmit)!
    }

    public override void Update() {
        base.Update();
        // Our scene is never at rest (it is always moving)!
        this.physicsSceneManager.isSceneAtRest = false;
    }
}
