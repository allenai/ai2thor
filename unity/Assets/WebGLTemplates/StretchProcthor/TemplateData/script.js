
$(

 

  () => {

    async function downloadAndProcessAssetTar(url) {
      const response = await fetch(url);
          if (!response.ok) throw new Error(`Failed to fetch ${url}`);
  
          const buffer = await response.arrayBuffer();
          const files = await untar(buffer);
  
          const assetId = url.split('/').pop().replace('.tar', '');
          const fileMap = {};
          files.forEach(file => {
              fileMap[file.name] = file;
          });
  
          const msgpackGzName = `${assetId}/${assetId}.msgpack.gz`;
          const msgpackFile = fileMap[msgpackGzName];
          if (!msgpackFile) {
              throw new Error(`Missing: ${msgpackGzName}`);
          }
  
          const assetData = msgpack.decode(pako.ungzip(new Uint8Array(msgpackFile.buffer)));
          // const assetData = JSON.stringify(decodedObject);
  
          const rawTextures = {};
          const textureMap = {
              'albedo.jpg': 'albedoBase64JPG',
              'metallic_smoothness.jpg': 'metallicSmoothnessBase64JPG',
              'normal.jpg': 'normalBase64JPG',
              'emission.jpg': 'emissionBase64JPG'
          };
  
          for (const [fileName, textureKey] of Object.entries(textureMap)) {
              const fullPath = `${assetId}/${fileName}`;
              if (fileMap[fullPath]) {
                  const base64 = arrayBufferToBase64(fileMap[fullPath].buffer);
                  rawTextures[textureKey] = `data:image/jpeg;base64,${base64}`;
              } else {
                  rawTextures[textureKey] = null;
              }
          }

          assetData["rawTextures"] = rawTextures

          delete assetData["albedoTexturePath"];
          delete assetData["metallicSmoothnessTexturePath"];
          delete assetData["normalTexturePath"];
          delete assetData["emissionTexturePath"];
          return assetData;
    }

    async function downloadAndProcessTars(urls) {
      const tasks = urls.map(async url => {
          return downloadAndProcessAssetTar(url);
      });
  
      return Promise.all(tasks);
  }
  
  
  function decompressMsgpackGz(buffer) {
      return new Promise((resolve, reject) => {
          try {

              console.log("--- buffer")
              console.log(buffer)
              // First, decompress gzip
              const decompressed = pako.ungzip(new Uint8Array(buffer));
              console.log("--- decomp gz")
              console.log(decompressed)
              // Then decode msgpack
              const decoded = msgpack.decode(decompressed);
              const jsonStr = JSON.stringify(decoded);
              resolve(jsonStr);
          } catch (err) {
              reject(err);
          }
      });
  }
  
  function arrayBufferToBase64(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
  
    const chunkSize = 8192;
    for (let i = 0; i < bytes.length; i += chunkSize) {
      binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
    }
  
    return btoa(binary);
  }

  function isHex(str) {
    return /^[0-9a-fA-F]+$/.test(str);
  }

  function getAllAssetIdsRecursively(objects, assetIds = []) {
    for (const obj of objects) {
        assetIds.push(obj.assetId);
        if (obj.children) {
          getAllAssetIdsRecursively(obj.children, assetIds);
        }
    }
    const assetSet = new Set(assetIds);
    assetSet.delete("");
    return Array.from(assetSet);
  }
  

  class WebProceduralAssetActionCallback {
    constructor(
      baseUrl,
      stopIfFalure=false,
      assetLimit=-1,
      extension=null
    ) {
      this.baseUrl = baseUrl;
      this.stopIfFalure = stopIfFalure;
      this.assetLimit = assetLimit;
      this.extension = extension;
    }

    // async getAssetsNotInDatabase(controller, assetIds) {
    //   console.log(` ========== getAssetsNotInDatabase`);
    //   let metadata = await controller.step({
    //     action:"AssetsInDatabase", 
    //     assetIds:assetIds, 
    //     updateProceduralLRUCache:false
    //   });
    //   console.log(` ========== getAssetsNotInDatabase result`);
    //   console.log(metadata);
    //   let assetsInDb = metadata.agents[0]["actionReturn"];
    //   const assetsIdsNotCreated = Object.entries(assetsInDb)
    //     .filter(([assetId, inDb]) => !inDb)
    //     .map(([assetId]) => assetId);
    //   return assetsIdsNotCreated;
    // }
    async getAssetsNotInDatabase(controller, assetIds) {
      let metadata = await controller.step({
        action:"AssetsInDatabase", assetIds:assetIds, updateProceduralLRUCache:false
      });
      let assetsInDb = metadata.agents[0]["actionReturn"];
      const assetsIdsNotCreated = Object.entries(assetsInDb)
        .filter(([assetId, inDb]) => !inDb && isHex(assetId))
        .map(([assetId]) => assetId);
      console.log("========== assetsIdsNotCreated");
      console.log(assetsIdsNotCreated);
      return assetsIdsNotCreated;
    }

    async downloadMissingAssets(controller, assetIds) {
      let assetsToDownload = await this.getAssetsNotInDatabase(controller, assetIds);
      console.log(` ========== assetsToDownload ${assetsToDownload}`);
      return downloadAndProcessTars(assetsToDownload.map((assetId) => `${this.baseUrl}/${assetId}.tar`));
    }

    // Throws stack overflow too much to create all assets in one go
    async createAllAssets(controller, assets) {
      return await controller.step({
        action: "CreateRuntimeAssets",
        assets: assets
      });


    }

    async createAssets(controller, assetIds) {
      // return await controller.step({
      //   action: "CreateRuntimeAssets",
      //   assets: assets
      // });

      let assetsToDownload = await this.getAssetsNotInDatabase(controller, assetIds);
      // assetsToDownload = assetsToDownload.slice(0, 1);
      console.log(` ========== assetsToDownload ${assetsToDownload}`);
      if (assetsToDownload.length > 0) {
        
        let loadingBar = document.querySelector("#unity-loading-bar");
        let progressBarFull = document.querySelector("#unity-progress-bar-full");
        let unityLogo = document.querySelector("#unity-logo");
        
        unityLogo.style.display = "none";
        loadingBar.style.display = "block";
        progressBarFull.style.width = 100 * 0.1 + "%";

        $("#downloading-text").removeClass('hidden');

        window.objaverseProgressCallback = (progress) => {

          progressBarFull.style.width = 100 * progress + "%";

        };
      let metadata = await controller.step({
        action: "DownloadAndCreateRuntimeAssets",
        baseUrl: this.baseUrl, 
        assetIds: assetsToDownload,
        reportProgressToJS: true
      });
      
      $("#downloading-text").text('Creating House');
      progressBarFull.style.width = 100 * 0.9 + "%";

      console.log(` ========== after DownloadAndCreateRuntimeAssets `);
      console.log(` success: ${metadata.agents[0].lastActionSuccess} message: ${metadata.agents[0].errorMessage}`);

    }

    let metadata = await controller.step({
      action: "UnloadUnusedAssets",
    });
    console.log(` ========== after UnloadUnusedAssets `);
    console.log(` success: ${metadata.agents[0].lastActionSuccess} message: ${metadata.agents[0].errorMessage}`);




      

      // let urls = assetsToDownload.map((assetId) => `${this.baseUrl}/${assetId}.tar`)
      // return downloadAndProcessTar();

      // let tasks = assetsToDownload.map((assetId) => {
      //     let url = `${this.baseUrl}/${assetId}.tar`;
      //     return downloadAndProcessAssetTar(url).then(
      //       (asset) => {
      //         return controller.step(
      //           {
      //             action: "CreateRuntimeAsset",
      //             asset: asset
      //           },
      //           true
      //         );
      //       }
      //     )
      // });
      // return Promise.all(tasks);
    }

    

    async Initialize(action, controller) {
      if (this.assetLimit > 0) {
        return await controller.step({
          action:"DeleteLRUFromProceduralCacheAsync", 
          assetLimit:self.asset_limit
        });
      }
      return null;
    }

    async CreateHouse(action, controller) {
      let house = action["house"];
      console.log(`========= assetlimit ${this.assetLimit}`);
      let assetIds = getAllAssetIdsRecursively(house["objects"], []);
      console.log(`========== CreateHouse callback`);
      console.log(assetIds);

      // This throws out of memory errors
      // console.log(`========== downloadMissingAssets callback`);
      // // let assets = await this.downloadMissingAssets(controller, assetIds);
      // console.log(`========== downloadMissingAssets result`);
      // console.log(assets);
      // return await this.createAssets(controller, assets);

      return await this.createAssets(controller, assetIds);

      // console.log(`========== inside CreateHouse callback`);

      // let assetIds = this.getAssetsIdsrecursively(house["objects"], []);
      // console.log(assetIds);

      // let result = await this.getAssetsNotInDatabase(controller, assetIds);
      // console.log(`========== inside CreateHouse callback after getAssetsNotInDatabase`);
      // return result;

      console.log(`============== getAssetsNotInDatabase2 `);
      let result = await this.getAssetsNotInDatabase(controller, this.getAllAssetIdsRecursively(action.house["objects"], []))
      // let result = await getAssetsNotInDatabase2(controller, getAllAssetIdsRecursively2(action.house["objects"], []))
      console.log(`============== getAssetsNotInDatabase2 result`);
      console.log(result);
    }

    async SpawnAsset(action, controller) {
      let assets = await this.downloadMissingAssets(controller, [action["assetId"]]);
      if (assets.length > 0) {
        return this.createAssets(controller, assets);
      }
      return null;
    }

  }
    
    class Controller {
      constructor(gameInstance, metadataHandler = (m) => {}, acionCallbackRunner = null, throwExceptionOnActionFail = false) {
        this.gameInstance = gameInstance;
        this.sequenceId = 0;
        this.rejectIfActionFailed = throwExceptionOnActionFail;
        this.actionHistory = [];
        this.stepAwaitPromises = {};
        this.acionCallbackRunner = acionCallbackRunner;


        window.onUnityMetadata = (stringMetadata) => {
          let metadata = JSON.parse(stringMetadata);
          var promise = this.stepAwaitPromises[metadata.sequenceId];
          if (promise !== undefined) {
            let agentMetadata = metadata.agents[0];
            let actionSuccess = agentMetadata.lastActionSuccess;
            if (throwExceptionOnActionFail && !actionSuccess) {
              promise.reject(metadata);
            }
            else {
              promise.resolve(metadata);
            }
            delete this.stepAwaitPromises[metadata.sequenceId];
          }

          // TODO decide if this should be called always?
          // this.stepAwaitPromises[met]
          let handled = metadataHandler(metadata);

          // Decide how you want to store the action here we store all, maybe only handled actions to filter stuff
          this.actionHistory.push(metadata);
        }
      }

      async step(action, passDataViaPointer=false) {
        if (this.acionCallbackRunner) {
          // let rawFunction = this.acionCallbackRunner[action.action];
          if (action.action in this.acionCallbackRunner){

            console.log(`============== Callback ${this.acionCallbackRunner[action.action]}`);
            let callback = (action, controller) => this.acionCallbackRunner[action.action](action, controller);

            if (callback) {

            // if (action.action === "CreateHouse") {
            //   console.log(`============== getAssetsNotInDatabase2 `);
            //   let result = await getAssetsNotInDatabase2(controller, getAllAssetIdsRecursively2(action.house["objects"], []))
            //   console.log(`============== getAssetsNotInDatabase2 result`);
            //   console.log(result);
            // }

              console.log(`========= calling hook ${action.action}`);
              let callbackPromise = callback(action, controller);
              console.log(callbackPromise);
              let callbackMetadata = await callbackPromise; 
              console.log(`========= return hook for ${action.action}`);
              console.log(callbackMetadata);
              
            }
          }
        }

        let prevSequenceId = this.sequenceId;
        this.sequenceId += 1;
        console.log(`------------- step Promise for ${action.action} prevSequenceId ${prevSequenceId} sequenceId ${this.sequenceId}`);
        
        let promise = new Promise((resolve, reject) => {
          this.stepAwaitPromises[prevSequenceId] = {
            resolve,
            reject,
            promise: this
          };

          if (!passDataViaPointer) {
          
            this.gameInstance.SendMessage ('FPSController', 'Step', JSON.stringify({
              ...action,
              sequenceId: prevSequenceId
            }));
          }
          else {

            // const encoder = new TextEncoder();
            // const byteArray = encoder.encode(JSON.stringify({
            //   ...action,
            //   sequenceId: prevSequenceId
            // }));
            // const length = byteArray.length;

            // const ptr = this.gameInstance.Module._malloc(length);
            // console.log(`============= pointer ${ptr} lenght ${length}`)
            // this.gameInstance.Module.HEAPU8.set(byteArray, ptr);

            // window._unityJsonBufferLength = length;

            // this.gameInstance.SendMessage ('FPSController', 'StepPointer', ptr);

            
            // const jsonString = JSON.stringify(JSON.stringify({
            //   ...action,
            //   sequenceId: prevSequenceId
            // }));

            const jsonString = JSON.stringify({
                  ...action,
                 sequenceId: prevSequenceId
              });

            // Encode JSON string as UTF-8
            const encoder = new TextEncoder();
            const encoded = encoder.encode(jsonString); // Uint8Array
            const length = encoded.length;

            console.log(`=========== pointer len ${length}`);

            // Allocate memory in Unity WASM heap
            const ptr = this.gameInstance.Module._malloc(length);

            console.log(`=========== pointer ${ptr}`);

            // Zero out the memory to avoid leftover garbage
            this.gameInstance.Module.HEAPU8.fill(0, ptr, ptr + length);

            // Set the memory with the JSON data
            this.gameInstance.Module.HEAPU8.set(encoded, ptr);

            // Save length for Unity to retrieve later
            window._unityJsonBufferLength = length;

            // Pass pointer to Unity
            this.gameInstance.SendMessage ('FPSController', 'StepPointer', ptr.toString());

          }
          
        });

        
        console.log(promise);
        // this.stepAwaitPromises[this.sequenceId].promise = promise;
        return promise;
      }
    }


    class ActionMetadataHandler {
      constructor(handlerMap) {
        this.handlerMap = handlerMap;
        console.log(this.handlerMap)
        this.lastMetadadta = null;
      }

      handleEvent(metadata) {
        let action = metadata.agents[0].lastAction;

        let handler = this.handlerMap[action];
        // console.log(`--------- handleEvent for ${action}, has handler: ${handler !== undefined}`);
        let handled = false;
        if (handler !== undefined) {
          // console.log(`--------- handleEvent for ${action}`);
          handler(metadata);
          handled = true;
        }
        let agentMetadata = metadata.agents[0];
        let agent = agentMetadata.agent;
        outputData.actions.push({
          lastAction: agentMetadata.lastAction,
          lastActionSuccess: agentMetadata.lastActionSuccess,
          agent: {
            x: agent.position.x,
            y: agent.position.y,
            z: agent.position.z,
            rotation: agent.rotation.y,
            horizon: agent.cameraHorizon,
            standing: agentMetadata.isStanding
          }
        });
        this.lastMetadadta = metadata;
        return handled;
      }

    }
    

    // Bunch of variables for state of the task
    let controller = null;
    let gameInstance = null;
    let getParams = parseGet();

    let actionHandler = null;

    // To support different wasm builds
    window.game_build = 'build' in getParams ? getParams['build'] : window.game_build;
    window.game_url = 'build' in getParams ? `${window.game_build}/Build/thor-local-WebGL.json` : window.game_url;
    console.log("GAME CONFIG: ", window.game_build_config);

    // This is a role based task so one could reuse something like this
    let hider = getParams['role'] !== 'seeker';
    let gameInitialized  = false;
    let objectId = '';
    
    let outputData = {
      object_type: getParams['object'],
      object_variation: getParams['variation'],
      open_objects: []
    };

    let houseId = getParams['house'];
    let gameConfig = null;
    let lastMetadadta = null;

    let isTurkSanbox = 'sandbox' in getParams && getParams['sandbox'].toLowerCase() === 'true';
    let distortionView = 'distortion' in getParams && getParams['distortion'].toLowerCase() === 'true'
    let distortionControls = 'distControls' in getParams && getParams['distControls'].toLowerCase() === 'true';
    let newHouses = 'newHouses' in getParams && getParams['newHouses'].toLowerCase() === 'true'
    const turkSandboxUrl = 'https://workersandbox.mturk.com/mturk/externalSubmit';
    const turkUrl = 'https://www.mturk.com/mturk/externalSubmit';

    let hasObject = false;

    // Utils
    function paramStrToAssocArray(prmstr) {
      let params = {};
      let prmarr = prmstr.split('&');
      for (let i = 0; i < prmarr.length; i++) {
        let tmparr = prmarr[i].split('=');
        params[tmparr[0]] = tmparr[1];
      }
      return params;
    }

    function parseGet() {
      let paramStr = window.location.search.substr(1);
      return paramStr !== null && paramStr !== ''
        ? paramStrToAssocArray(paramStr)
        : {};
    }

    /////////////////////
    ///// Unity callbacks
    window.onGameLoaded = function() {
      if (!gameInitialized) {
        if ('scene' in getParams && getParams['scene']) {
          gameInstance.SendMessage('PhysicsSceneManager', 'SwitchScene', getParams['scene']);
        }
        gameInitialized = true;

        /// Another place for initialization code, like Initialize or LoadHouse
      }
    };

    // Aggregate data
    function gatherFinalState(metadata) {

      // Example from hideNseek
      let agentMetadata = metadata.agents[0];
      let filtered = agentMetadata.objects.filter((obj) => obj.objectId === objectId);
      if (filtered.length === 1) {
        let object = filtered[0];

        outputData['object_position'] = object.position;
        outputData['object_rotation'] = object.rotation;
        outputData['open_objects'] = agentMetadata.objects.filter((obj) => obj.isOpen).map(obj => obj.objectId);

        outputData['object_locations_and_rotations'] = agentMetadata.objects.reduce((acc, obj, {}) => {
          return {
            ...acc,
            [obj.objectId]:{
              position: obj.position,
              rotation: obj.rotation
            }
          }
        });
      }
      else {
        throw `Invalid id ${objectId} in metadata.objects: ${agentMetadata.objects}`;
      }
      return outputData;
    }

    // Submit to turk
    function submitHit(metadata) {
      let data = gatherFinalState(metadata);
      document.forms['mturk_form'].assignmentId.value = getParams['assignmentId'];
      console.log('Turk submit!!', data);
      document.forms['mturk_form'].data.value = JSON.stringify(data);
      // document.forms['mturk_form'].submit();
      window.parent.postMessage(JSON.stringify(data), '*');
    }

    ///////////////////////
    ///// Hider's Handlers

    // Stores the trayectory of agent
    function Move(metadata) {
      outputData.trayectory.push(metadata.agents[0].agent.position);
    }


    /////////////////////////
    ///// Initialization Functions

    async function LoadProcthorHouse(house_name) {


      console.log("------------ LoadProcthorHouse");
      console.log(house_name);
      let url = newHouses ? `https://thor-turk.s3.us-west-2.amazonaws.com/houses/val/${house_name}.json.gz` : `https://thor-turk.s3.us-west-2.amazonaws.com/houses/${house_name}.json`;
      console.log(`======= url ${url}`);
      let house = null;
      if (url.endsWith(".json")) {
        house = await $.getJSON(
        // `TemplateData/houses/${house_name}`,
        url,
        );
      }
      else if (url.endsWith(".gz")) {

          const response = await fetch(url);
          if (!response.ok) throw new Error(`Failed to fetch ${url}`);

          const compressedData = new Uint8Array(await response.arrayBuffer());
          const decompressedData = pako.ungzip(compressedData, { to: 'string' });

          house = JSON.parse(decompressedData);

      }
      
        console.log("----- read house");
        console.log(house);
        
        console.log("----- PreloadHouseAssets");

        // let metadata = await controller.step({
        //   "action": "PreloadHouseAssets",
        //   "house": house,
        //   "freeMemoryAfter": true,
        //   freeMemorySecondsTimeout: 2.0
        // });

        // console.log(metadata);


        metadata = await controller.step({
          "action": "CreateHouse",
          "house": house,
          "sequenceId": 1
        })

        console.log("--- CreateHouse ");
        console.log(metadata);

        metadata = await controller.step({
          action: "UnloadUnusedAssets",
        });
        console.log(` ========== after UnloadUnusedAssets `);
        console.log(` success: ${metadata.agents[0].lastActionSuccess} message: ${metadata.agents[0].errorMessage}`);
    
        // metadata = await controller.step({
        //   action:"DeleteLRUFromProceduralCache", 
        //   assetLimit: 0
        // });

        // console.log(` ========== after DeleteLRUFromProceduralCache `);
        // console.log(` success: ${metadata.agents[0].lastActionSuccess} message: ${metadata.agents[0].errorMessage}`);
    

        let agent = house["metadata"]["agent"];
        metadata = await controller.step({
          "action": "TeleportFull",
            x: agent["position"]["x"],
            y: agent["position"]["y"],
            z: agent["position"]["z"],
            rotation: agent["rotation"],
            horizon: agent["horizon"],
            standing: agent["standing"],
            forceAction: true,
        });

        console.log("--- TeleportFull ");
        console.log(metadata);

    }

    async function getTopDownCameraParams(skyboxColor="black", orthographic=false) {
      let generalMetadata = await controller.step({"action": "GetMapViewCameraProperties"});
      let metadata = generalMetadata.agents[0];
      let cam = metadata["actionReturn"];
      let bounds = metadata["sceneBounds"]["size"];
      let max_bound = Math.max(bounds["x"], bounds["z"]);

      cam["fieldOfView"] = 50;
      cam["position"]["y"] += 1.1 * max_bound;
      cam["orthographic"] = orthographic;
      cam["farClippingPlane"] = 50;
      cam["skyboxColor"] = skyboxColor;
      // cam["position"]["y"] = 
      // cam["viewPort"] = {x: 2/3, y: 0.0, width: 1/3, height: 0.5};
      if (!orthographic) {
        delete cam["orthographicSize"];
      }
      

     

      console.log(`======== GetMapViewCameraProperties ${metadata}`);
      return cam;
    }


    function calculateViewPorts(arrangement) {
      let viewports = [];
      let row = 0;
      let rows = arrangement.length;
      for (const columns of arrangement) {

        for (let column = 0; column < columns; column++) {
          viewports.push(
            {x: (column)/columns, y: (rows-(row+1))/rows, width: 1/columns, height: 1/rows}
          );
        }
        row++;
      }
      return viewports;
    }
    

    async function InitializeArm() {
      console.log("--- Initialize ");

      
      let fov = 120;

      // MOUNTED_CAMERA_POSITIONS = [
      //   {
      //       "position": {"x": -0.1211464, "y": 0.561659, "z": 0.03892733},
      //       "rotation": {"x": 20.0, "y": 0.0, "z": 0.0},
            
      //       // Top left
      //       "viewPort": {x: 0.0, y: 0.5, width: 0.5, height: 0.5},
      //   },
      //   {
      //       "position": {"x": 0.03036952, "y": 0.5616351, "z": 0.001642581},
      //       "rotation": {"x": 20.0, "y": 120.0, "z": 0.0},
      //       // "thirdPartyCameraId": 0,
      //       // top right
      //       "viewPort": {x: 0.5, y: 0.5, width: 0.5, height: 0.5},
      //   },
      //   {
      //       "position": {"x": -0.1507564, "y": 0.5616323, "z": -0.1055215},
      //       "rotation": {"x": 20.0, "y": -120.0, "z": 0.0},
      //       // "thirdPartyCameraId": 1,
      //       // Bottom left
      //       "viewPort": {x: 0.0, y: 0.0, width: 0.5, height: 0.5},
      //   },
      //   {
      //       "position": {"x": -0.08022631, "y": 0.40808, "z": -0.00178992},
      //       "rotation": {"x": 90.0, "y": 0.0, "z": 0.0},
      //       // "thirdPartyCameraId": 2,

      //       // Bottom right
      //       "viewPort": {x: 0.5, y: 0.0, width: 0.5, height: 0.5},
            
      //   },
      // ]

      let progressBarFull = document.querySelector("#unity-progress-bar-full");
      $("#downloading-text").text('Setting Views');
      progressBarFull.style.width = 100 * 0.93 + "%";

      // let viewports = calculateViewPorts([2,2,1]);
      let viewports = calculateViewPorts([2,2]);
      console.log(`======== arrangement`);
      console.log(viewports);

      MOUNTED_CAMERA_POSITIONS = [
        {
            "position": {"x": -0.1211464, "y": 0.561659, "z": 0.03892733},
            "rotation": {"x": 20.0, "y": 0.0, "z": 0.0},
            
            // Top left
            // "viewPort": {x: 0.0, y: 0.5, width: 0.5, height: 0.5},
        },
        

        {
            "position": {"x": 0.03036952, "y": 0.5616351, "z": 0.001642581},
            "rotation": {"x": 20.0, "y": 120.0, "z": 0.0},
            // "thirdPartyCameraId": 0,
            // top right
            // "viewPort": {x: 0.5, y: 0.5, width: 0.5, height: 0.5},

            'fieldOfView': fov, 
            "overwriteRGBWithDistortion": distortionView,
            'agentPositionRelativeCoordinates': true, 
            'parent': 'agent',
        },
        {
            "position": {"x": -0.1507564, "y": 0.5616323, "z": -0.1055215},
            "rotation": {"x": 20.0, "y": -120.0, "z": 0.0},
            // "thirdPartyCameraId": 1,
            // Bottom left
            // "viewPort": {x: 0.0, y: 0.0, width: 1/3, height: 0.5},
            'fieldOfView': fov, 
            "overwriteRGBWithDistortion": distortionView,
            'agentPositionRelativeCoordinates': true, 
            'parent': 'agent',
        },
        {
            "position": {"x": -0.08022631, "y": 0.40808, "z": -0.00178992},
            "rotation": {"x": 90.0, "y": 0.0, "z": 0.0},
            // "thirdPartyCameraId": 2,

            // Bottom Center
            // "viewPort": {x: 1/3, y: 0.0, width: 1/3, height: 0.5},
            'fieldOfView': fov, 
            "overwriteRGBWithDistortion": distortionView,
            'agentPositionRelativeCoordinates': true, 
            'parent': 'agent',
            
        },

        // {
        //   // ...(await getTopDownCameraParams(skyboxColor="#99BCFCFF", orthographic=true))
        //   ...(await getTopDownCameraParams(skyboxColor="#64676EFF", orthographic=true))
        // },
        // {
        //   ...(await getTopDownCameraParams())
        // }
      ]


            let metadata = await controller.step({
              "action": "Initialize",
              "agentMode": "stretch",
              "agentControllerType": "stretch",
              "visibilityScheme": "Distance",
              // "renderInstanceSegmentation": true,
              // "renderDepth": true,
              //                  action["antiAliasing"] = "smaa";
              "massThreshold": 10.0,
              "renderDistortionImage": distortionView,
              "overwriteRGBWithDistortion": distortionView
          });
          progressBarFull.style.width = 100 * 0.96 + "%";

           
            console.log(metadata);
            console.log("--- After Initialize ");

           
            let index = 0;
            for (const cam of MOUNTED_CAMERA_POSITIONS) {

              console.log(cam);
              console.log(`===== ${index} ${"thirdPartyCameraId" in cam}`);

              if (index == 0) {

                metadata = await controller.step({
                    action: "UpdateMainCamera",
                    // viewPort: cam.viewPort,

                    viewPort: viewports[index],
                    position: cam.position,
                    rotation: cam.rotation,
                    fieldOfView: fov,
                    
                    agentId:0
                  }
                );

              }
              else  {
                console.log("--- Call AddThirdPartyCamera ");
                // metadata = await controller.step(
                //   {
                //     'action': 'AddThirdPartyCamera', 
                //     'agentPositionRelativeCoordinates': true, 
                //     'fieldOfView': fov, 
                //     'parent': 'agent', 
                //     'position': cam["position"], 
                //     'rotation': cam["rotation"],
                //     "targetDisplay": 0,
                //     "viewPort": cam.viewPort,
                //     "overwriteRGBWithDistortion": distortionView
                //   }
                // );

                metadata = await controller.step(
                  {
                    action: 'AddThirdPartyCamera',  
                    targetDisplay: 0,
                    viewPort: viewports[index],
                    ...cam
                  }
                );
              }

              

              console.log(`--- ${index} ${metadata.agents[0].lastAction}`);
              console.log(metadata);
              index++;

            }
            
            if (distortionView) {
              metadata = await controller.step({
                "action": "SetDistortionShaderParams",
                "zoomPercent": 0.49,
                "k1": 0.9,
                "k2": 5.2,
                "k3": -13.0,
                "k4": 16.3,
                "intensityX": 1.0,
                "intensityY": 0.98,
                // Replace with all thirdparty camera indices
                "thidPartyCameraIndices": [0, 1, 2, 3]
              });

              console.log(`--- ${metadata.agents[0].lastAction}`);
              console.log(metadata);
          }

          progressBarFull.style.width = 100 + "%";
          
          // Deletes all the object even instantiated ones :)
        //   metadata = await controller.step({
        //   action:"DeleteLRUFromProceduralCache", 
        //   assetLimit: 0
        // });

        // console.log(` ========== after DeleteLRUFromProceduralCache `);
        // console.log(` success: ${metadata.agents[0].lastActionSuccess} message: ${metadata.agents[0].errorMessage}`);
    

    //         "zoomPercent": 0.49,
    // "k1": 0.9,
    // "k2": 5.2,
    // "k3": -13.0,
    // "k4": 16.3,
    // "intensityX": 1.0,
    // "intensityY": 0.98,

            //generalMetadata = controller.step({"action": "AddThirdPartyCamera", "skyboxColor": "white", **cam})

            // MOUNTED_CAMERA_POSITIONS.forEach(async (cam, index) => {

            //   console.log(`--- ${index} ${"thirdPartyCameraId" in cam}`);

            //   if (!"thirdPartyCameraId" in cam) {

            //     metadata = await controller.step({
            //         action: "UpdateMainCamera",
            //         viewPort: cam.viewPort,
            //         position: cam.position,
            //         rotation: cam.rotation,
            //         fieldOfView: fov,
            //         agentId:0
            //       }
            //     );

            //   }
            //   else {

            //       metadata = await controller.step(
            //       {
            //         'action': 'AddThirdPartyCamera', 
            //         'agentPositionRelativeCoordinates': true, 
            //         'fieldOfView': fov, 
            //         'parent': 'agent', 
            //         'position': cam.position, 
            //         'rotation': cam.rotation,
            //         "targetDisplay": 0,
            //         "viewPort": cam.viewPort,
            //         "overwriteRGBWithDistortion": distortionView,
            //         "thirdPartyCameraId": cam.thirdPartyCameraId
            //       }
            //     );

            //   }

            //   console.log(`--- ${index} ${metadata.agents[0].lastAction}`);
            //   console.log(metadata);

            // });
          
          // metadata = await controller.step({
          //   "action": "UpdateMainCamera",
          //   "viewPort": {x: 0.0, y: 0.5, width: 0.5, height: 0.5},
          //   }
          // );

          // console.log("--- UpdateMainCamera ");
          // console.log(metadata);
          // console.log(`===== distortionView ${distortionView}`);
          // console.log("--- Call AddThirdPartyCamera ");
          //   metadata = await controller.step(
          //     {
          //       'action': 'AddThirdPartyCamera', 
          //       'agentPositionRelativeCoordinates': true, 
          //       'fieldOfView': 139, 
          //       'parent': 'agent', 
          //       'position': {'x': 0.04, 'y': 0.5560812, 'z': 0.0}, 
          //       'rotation': {'x': 30.0, 'y': 120.0, 'z': 0.0},
          //       "targetDisplay": 0,
          //       "viewPort": {x: 0.5, y: 0.5, width: 0.5, height: 0.5},
          //       "overwriteRGBWithDistortion": distortionView
          //     }
          //   );
          //   console.log("--- AddThirdPartyCamera ");
          //   console.log(metadata);

          //   metadata = await controller.step(
          //     {
          //       'action': 'AddThirdPartyCamera', 
          //       'agentPositionRelativeCoordinates': true, 
          //       'fieldOfView': 139, 
          //       'parent': 'agent', 
          //       'position': {'x': -0.04, 'y': 0.5560812, 'z': 0.0}, 
          //       'rotation': {'x': 30.0, 'y': 225.0, 'z': 0.0},
          //       "targetDisplay": 0,
          //       "viewPort": {x: 0.5, y: 0.0, width: 0.5, height: 0.5},
          //       "overwriteRGBWithDistortion": distortionView
          //     }
          //   );
          //   console.log("--- AddThirdPartyCamera 2");
          //   console.log(metadata);

        let loadingBar = document.querySelector("#unity-loading-bar");
        // let progressBarFull = document.querySelector("#unity-progress-bar-full");

        loadingBar.style.display = "none";
    }

    async function MoveAgent(ahead, right) {

      await controller.step({
        action: "MoveAgent",
        returnToStart: true,
        ahead: ahead,
        right: right,
        speed: 4,
        "physicsSimulationParams": {"autoSimulation": true}
      });
      
    }

    function setDistortionControls() {
      const parameters = [
        // { name: "_LensDistortionStrength", label: "Lens Distortion Strength", min: -20.0, max: 20.0, value: 1.0 },
        // { name: "_LensDistortionTightness", label: "Lens Distortion Power", min: -20.0, max: 20.0, value: 7.0 },
        { name: "_ZoomPercent", label: "Zoom Percent", min: 0.0, max: 5.0, value: 1.0 },
        { name: "_k1", label: "K1 polynomial dist coeff", min: -8.0, max: 8.0, value: -0.126 },
        { name: "_k2", label: "K2 polynomial dist coeff", min: -8.0, max: 8.0, value: 0.004 },
        { name: "_k3", label: "K3 polynomial dist coeff", min: -18.0, max: 18.0, value: 0.0 },
        { name: "_k4", label: "K4 polynomial dist coeff", min: -18.0, max: 18.0, value: 0.0 },
        { name: "_DistortionIntensityX", label: "Distort Strength X", min: 0.0, max: 6.0, value: 1.0 },
        { name: "_DistortionIntensityY", label: "Distort Strength Y", min: 0.0, max: 6.0, value: 1.0 }
      ];
      
      function createControl(param) {
        return `
          <div class="shader-control" data-name="${param.name}">
            <div class="shader-label">${param.label} (${param.name})</div>
            <div class="shader-slider-container">
              <input type="range" 
                     class="shader-slider" 
                     min="${param.min}" 
                     max="${param.max}" 
                     step="0.001" 
                     value="${param.value}">
              <input type="number" 
                     class="shader-number-input" 
                     min="${param.min}" 
                     max="${param.max}" 
                     step="0.001" 
                     value="${param.value}">
            </div>
          </div>
        `;
      }
      

      function getShaderParams() {
        const container = $('#shader-controls');
      
        return {
          action: "SetDistortionShaderParams",
          zoomPercent: parseFloat(container.find('[data-name="_ZoomPercent"] .shader-number-input').val()),
          k1: parseFloat(container.find('[data-name="_k1"] .shader-number-input').val()),
          k2: parseFloat(container.find('[data-name="_k2"] .shader-number-input').val()),
          k3: parseFloat(container.find('[data-name="_k3"] .shader-number-input').val()),
          k4: parseFloat(container.find('[data-name="_k4"] .shader-number-input').val()),
          // strength: parseFloat(container.find('[data-name="_LensDistortionStrength"] .shader-number-input').val()),
          intensityX: parseFloat(container.find('[data-name="_DistortionIntensityX"] .shader-number-input').val()),
          intensityY: parseFloat(container.find('[data-name="_DistortionIntensityY"] .shader-number-input').val()),
          thidPartyCameraIndices: [0, 1, 2]
        };
      }
      async function updateShaderParams() {
        const params = getShaderParams();
        await controller.step(params);
      }
      
      const container = $('#shader-controls');
      parameters.forEach(param => {
        container.append(createControl(param));
      });
    
      container.on('input', '.shader-slider', async function () {
        const value = $(this).val();
        const numberInput = $(this).closest('.shader-slider-container').find('.shader-number-input');
        numberInput[0].value = value; // Avoid triggering input event
        await updateShaderParams();
      });
    
      // When number input is changed (on blur or enter), update slider and send update
      container.on('change', '.shader-number-input', async function () {
        const value = $(this).val();
        const slider = $(this).closest('.shader-slider-container').find('.shader-slider');
        slider[0].value = value; // Update slider silently
        await updateShaderParams();
      });
    }




    // Initialization code here
    async function InitScene(metadata) {

      outputData['scene'] = metadata.agents[0].sceneName;
      outputData['trayectory'] = [];
      outputData['actions'] = [];

      // TODO black screen issue in webgl when calling initialize before loading the house so always load house first
      console.log("-- LoadProcthorHouse");
      // await LoadProcthorHouse("procthor_train_1.json");
      // await LoadProcthorHouse(houseId ? houseId : "house_val_0");
      await LoadProcthorHouse(houseId ? houseId : "house_val_0");
      console.log("-- Initialize arm");
      await InitializeArm();

     

      $("#up").click(async (e) => {
        console.log("-------up click");
        $("#up").attr("disabled", true).addClass('pressed');
        // $("#down").attr("disabled", true)

        $(".arrow-keys > *").attr("disabled", true).addClass('soft');
        $("#up").attr("disabled", true).addClass('pressed').removeClass('soft');
        // $(".arrow-keys button > *").each((el) => { 
        //   console.log(el);
        //   el.attr("disabled", true)});
        await MoveAgent(1, 0);
        $("#up").removeClass('pressed').attr("disabled", false);
        // $(".arrow-keys > *").each((el) => { el.attr("disabled", false)});
        $(".arrow-keys > *").attr("disabled", false).removeClass('soft');
      });

      $("#down").click(async (e) => {
        $(".arrow-keys > *").attr("disabled", true).addClass('soft');
        $("#down").removeClass('soft').addClass('pressed');
        await MoveAgent(-1, 0);
        $("#down").removeClass('pressed').attr("disabled", false);
        $(".arrow-keys > *").attr("disabled", false).removeClass('soft');
      });
      $("#right").click(async (e) => {
        $(".arrow-keys > *").attr("disabled", true).addClass('soft');
        $("#right").removeClass('soft').addClass('pressed');
        await MoveAgent(0, 1);
        $("#right").removeClass('pressed').attr("disabled", false);
        $(".arrow-keys > *").attr("disabled", false).removeClass('soft');
      });
      $("#left").click(async (e) => {
        $(".arrow-keys > *").attr("disabled", true).addClass('soft');
        $("#left").removeClass('soft').addClass('pressed');
        await MoveAgent(0, -1);
        $("#left").removeClass('pressed').attr("disabled", false);
        $(".arrow-keys > *").attr("disabled", false).removeClass('soft');
      });

      $('.arrow-keys button').prop('disabled', false);
      
      

      // in Get params
      let objectName = getParams['object'];
      console.log(`objectname: ${objectName}`)
    }

    if (distortionView && distortionControls) {
      setDistortionControls();
      $("#set-distortion").click(async (e) => {
        console.log("-------set-distortion");
          await controller.step({
            "action": "SetDistortionShaderParams",
            "zoomPercent": 0.84,
            "k1": 0.1,
            "k2": 0.01,
            "k3": -0.2,
            "k4": 0.0,
            "strength": 1.0,
            "intensityX": 1.0,
            "intensityY": 1.0,
            // Replace with all thirdparty camera indices
            "thidPartyCameraIndices": [0, 1, 2]
          });
      });
    }
    
    // This is unused but if we want to hijack Webgl rendering for stuff
    function startPostRenderLoop(gl) {
      function blitAfterUnity() {
        requestAnimationFrame(blitAfterUnity); // Sync with Unity’s frame rate
        console.log("---- render");
        gl.clearColor(0.0, 0.5, 0.0, 1.0);
        gl.clear(gl.COLOR_BUFFER_BIT);
      }
      requestAnimationFrame(blitAfterUnity);
    }


    function InitializeUnity(url) {
      let loadingBar = document.querySelector("#unity-loading-bar");
      let progressBarFull = document.querySelector("#unity-progress-bar-full");

      loadingBar.style.display = "block";
      progressBarFull.style.width = 100 * 0.1 + "%";

      createUnityInstance(document.querySelector("#unity-canvas"), window.game_build_config,
      (progress) => {

        progressBarFull.style.width = 100 * progress + "%";
    
      }).then((instance) => {
          loadingBar.style.display = "none";
          console.log("game Loaded");
          gameInstance = instance;
          window.gameInstance = gameInstance;

          $("#unity-canvas").focus();

          // Code for autofocusing the canvas since WebGLInput.captureAllKeyboardInput = false is called on unity, so that the canvas does not 
          // absorb all input events
          document.addEventListener("focusout", (e) => {
            const tag = e.target.tagName;
            if (tag === "INPUT" || tag === "TEXTAREA") {
              // Delay to avoid stealing focus from anything else triggered
              setTimeout(() => canvas.focus(), 0);
            }
          });
      
          // Focus canvas after clicking any button
          document.addEventListener("click", (e) => {
            const tag = e.target.tagName;
            if (tag === "BUTTON" || e.target.classList.contains("refocus-canvas")) {
              setTimeout(() => canvas.focus(), 0);
            }
          });
      
          // Optionally, add a global keyboard shortcut (e.g., Escape) to refocus Unity
          document.addEventListener("keydown", (e) => {
            if (e.key === "Escape") {
              canvas.focus();
            }
          });
          
          
          actionHandler = new ActionMetadataHandler(
            handlerMap = {
                // This is the First action called by unity after loading, a null action, so initialization code here
                [null]: InitScene,
        
                // Depends on what move we end up using
                MoveAhead: Move,
                MoveBack: Move,
                MoveLeft: Move,
                MoveRight: Move,
            }
          );

          controller = new Controller(
            gameInstance = gameInstance, 
            metadataHandler = (m) => actionHandler.handleEvent(m), 
            acionCallbackRunner = new WebProceduralAssetActionCallback(
              baseUrl="https://pub-2619544d52bd4f35927b08d301d2aba0.r2.dev/assets",
              stopIfFalure=false,
              assetLimit=-1,
              extension=null
            ),
            throwExceptionOnActionFail = false
          );

          controller.step(
            {
              action: "SetDefaultPhysicsSimulationParams", 
              defaultPhysicsSimulationParams: {
                autoSimulation: true,
                fixedDeltaTime: 0.02
              }
            }
          )

          console.log(gameInstance);


          // To hijack rendering
          //startPostRenderLoop(gameInstance.Module.ctx);

          // if (typeof gameInstance.Module !== 'undefined' && gameInstance.Module.canvas) {
          //   gameInstance.Module.keyboardEventTarget = document;
      
          //   const inputTags = ['INPUT', 'TEXTAREA'];
      
          //   // Prevent Unity from capturing keyboard events
          //   const allowNormalInput = function (e) {
          //     const activeTag = document.activeElement.tagName;
          //     const isTyping = inputTags.includes(activeTag);
      
          //     if (isTyping) {
          //       // Let the browser handle key normally
          //       e.stopPropagation();
          //       return true;
          //     }
          //   };
      
          //   document.addEventListener('keydown', allowNormalInput, true);
          //   document.addEventListener('keypress', allowNormalInput, true);
          //   document.addEventListener('keyup', allowNormalInput, true);
          //   document.addEventListener('input', allowNormalInput, true);
          // }
      }); 
    }


    // Unity Loader Script
    $.getScript(`${window.loaderUrl}` )
      .done(function( script, textStatus ) {

         $("#test-tar").click(async (e) => {

        try {
          const results = await downloadAndProcessTars([
              "Build/assets/000074a334c541878360457c672b6c2e.tar"
          ]);
          console.log('Processed assets:', results);
        } catch (error) {
            console.error('Error processing tar files:', error);
        }

      });


        console.log("Status: ", textStatus);

        $("#mturk_form").attr("action", isTurkSanbox ? turkSandboxUrl : turkUrl);


        // $("#switch-procedural").click((e) => {
        //   gameInstance.SendMessage('PhysicsSceneManager', 'SwitchScene', 'Procedural');
          
        // });
        


        // Instruction Rendering
        if (hider) {
            let objectHTML = $('<strong class="important-text"></strong>');
            objectHTML.text(getParams['object']);
            $("#instruction-text").append("You have to Hide a ");
            $("#instruction-text").append(objectHTML);
            $("#instruction-2").html(`Move around in the room, open drawers and cabinets to look for a good hiding spot.`);
            $("#instruction-3").html('When you are ready, move the object (see Shift controls) to place it more precisely, click on it to drop it.');
            $("#instruction-4").html(`If you're happy with your hiding spot click the <strong class="green-text">Finish</strong> button above. Or <strong class="red-text">Reset</strong> to start over.`);
             $("#instruction-5").html("If a door/drawer opens and closes, it means you're in the way! Move back and try again.");
             $("#instruction-6").html("If you're clicking on the object and it's not being picked up, try moving closer to the object! (within 1.5 meters)");
            $("#instructions-hider").show();

            $(document).keypress(function(event) {
                if (event.keyCode === 32) {
                if(event.shiftKey && !hasObject){
                    pickupFailTimeout = setTimeout(() => {
                        $("#last-action-text").html(`Action Failed: <strong class="red-text">Pick Up Failed</strong>`).show();
                    }, 800)
                } else {

                }
                }
            });
          InitializeUnity(window.game_url);
        }
        else {
          // Uses a hider game data to create a game
          $.getJSON(
              `https://thor-turk.s3-us-west-2.amazonaws.com/hide-n-seek/data/hider/${getParams['config']}`,
              function(config) {
                  console.log("------ Config", config);
                  gameConfig = config;
                  $("#instructions-seeker").show();
                  $("#message-text").show();
                  // gameConfig['agentPosition']
                  let objectHtml = `<strong class="important-text">${gameConfig.object_type}</strong>`;
                  $("#instruction-text").html(`You have to find a ${objectHtml}`);
                  $("#instruction-2").html(`Move around in the room, open drawers and cabinets to look for a ${objectHtml}.`);
                  $("#instruction-3").html('Click on it once you have found it.');
                  $("#instruction-4").html(`If you cannot find the ${objectHtml} after some time, you can click the <strong class="red-text">Give Up</strong> button above.`);
                  getParams['object'] = gameConfig['object_type'];
                  getParams['variation'] = gameConfig['object_variation'];

                  outputData.object_type = gameConfig['object_type'];
                  outputData.object_variation = gameConfig['object_variation'];

                  $("#finish-hit").click((e) => {
                        outputData['success'] = false;
                        submitHit(lastMetadadta);

                  }).text("Give Up").toggleClass("giveup-btn");


                  $("#reset-hit").hide();

                  setTimeout(() => {
                       $("#finish-hit").attr("disabled", false);
                  }, 'giveUpEnableSeconds' in getParams ? parseInt(getParams['giveUpEnableSeconds']) * 1000 : 30000);

                  // Starting point for unity
                  InitializeUnity(window.game_url);
           });
        }
      })
      .fail(function( jqxhr, settings, exception ) {
        console.error( "Triggered ajaxError handler.", exception);
    });
  }
);



