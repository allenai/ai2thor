
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System;
using UnityEngine;
using UnityEngine.TestTools;
using UnityStandardAssets.Characters.FirstPerson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Tests {
    public class TestBase {
        protected int sequenceId = 0;
        protected bool lastActionSuccess;
        protected string error;
        protected object actionReturn;

        protected MultiAgentMetadata metadata; // = new MultiAgentMetadata(); 

        protected List<KeyValuePair<string, byte[]>> renderPayload;

        protected ThirdPartyCameraMetadata[] cameraMetadata;

        public IEnumerator step(Dictionary<string, object> action) {
            
            var agentManager = GameObject.FindObjectOfType<AgentManager>();
            action["sequenceId"] = sequenceId;
            agentManager.ProcessControlCommand(new DynamicServerAction(action));
            yield return new WaitForEndOfFrame();
            this.generateMetadata();
            // yield return agentManager.EmitFrame();
            var agent = agentManager.GetActiveAgent();
            lastActionSuccess = agent.lastActionSuccess;
            actionReturn = agent.actionReturn;
            error = agent.errorMessage;
            sequenceId++;
        }

        [SetUp]
        public virtual void Setup() {
            // metadata = new MultiAgentMetadata();
            UnityEngine.SceneManagement.SceneManager.LoadScene("FloorPlan1_physics");
        }

        public virtual IEnumerator Initialize() {
            Dictionary<string, object> action = new Dictionary<string, object>() {
                { "gridSize", 0.25f},
                { "agentCount", 1},
                { "fieldOfView", 90f},
                { "snapToGrid", true},
                { "action", "Initialize"}
            };
            yield return step(action);
        }
        

        protected AgentManager agentManager {
            get => GameObject.FindObjectOfType<AgentManager>();
        }

        protected MetadataWrapper getLastActionMetadata(int agentId = -1) {
            var id = agentId == -1 ? this.agentManager.GetActiveAgentId() : agentId;
            return this.metadata.agents[id];
        }

        protected void savePng(byte[] img, string filePath) {
            var pngBytes = UnityEngine.ImageConversion.EncodeArrayToPNG(img, agentManager.tex.graphicsFormat, (uint)UnityEngine.Screen.width, (uint)UnityEngine.Screen.height);
            
            System.IO.File.WriteAllBytes(filePath, pngBytes);
        }

        private void generateMetadata() {
            MultiAgentMetadata multiMeta = new MultiAgentMetadata();
            ThirdPartyCameraMetadata[] cameraMetadata = new ThirdPartyCameraMetadata[agentManager.GetThirdPartyCameraCount()];
            List<KeyValuePair<string, byte[]>> renderPayload = new List<KeyValuePair<string, byte[]>>();
            agentManager.createPayload(multiMeta, cameraMetadata, renderPayload, true);
            this.renderPayload = renderPayload;
            this.cameraMetadata = cameraMetadata;
            this.metadata = multiMeta;
        }
    }
}
