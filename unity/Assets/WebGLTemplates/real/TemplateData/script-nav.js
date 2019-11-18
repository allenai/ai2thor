$(
  () => {
    let gameInstance = null;
    let getParams = parseGet();
    window.game_build = 'build' in getParams ? getParams['build'] : window.game_build;
    window.game_url = 'build' in getParams ? `${window.game_build}/Build/thor-local-WebGL.json` : window.game_url;

    console.log("GAME URL: ", window.game_url);
    let gameInitialized  = false;
    let objectId = '';

    if (!('object' in getParams)) {
      throw `Must provide an 'object' url parameter as target object, but none was provided`;
    }
    let objectType = getParams['object'];



    let outputData = {
      object_type: getParams['object'],
      reset_count: 0
    };
    let lastMetadadta = null;

    let isTurkSanbox = 'sandbox' in getParams && getParams['sandbox'].toLowerCase() === 'true';
    const turkSandboxUrl = 'https://workersandbox.mturk.com/mturk/externalSubmit';
    const turkUrl = 'https://www.mturk.com/mturk/externalSubmit';

    let angleStepDegrees = 'angle' in getParams ? parseFloat(getParams['angle']) : 90;

    let agentPosition =  {
      x: parseFloat('x' in getParams ? getParams['x'] : "0.0"),
      y: parseFloat('y' in getParams ? getParams['y'] : "0.0"),
      z: parseFloat('z' in getParams ? getParams['z'] : "0.0")
    };

    let agentRotation = {
      x: parseFloat('x_rot' in getParams ? getParams['x_rot'] : "0.0"),
      y: parseFloat('y_rot' in getParams ? getParams['y_rot'] : "0.0"),
      z: parseFloat('z_rot' in getParams ? getParams['z_rot'] : "0.0")
    };

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
      }
    };

    window.onUnityMetadata = function(metadata) {
      let jsonMeta = JSON.parse(metadata);
      // FIRST init event
      handleEvent(jsonMeta);
      lastMetadadta = jsonMeta;
    };

    // Aggregate data
    function gatherFinalState(metadata) {
      let agentMetadata = metadata.agents[0];
      checkIfObjectVisible(objectId, agentMetadata);
      console.log("------- Final is visible? " + outputData.success," Object ID ", objectId);
      let agent = agentMetadata.agent;
      outputData.agent_finish_location = {
        x: agent.position.x,
        y: agent.position.y,
        z: agent.position.z,
        rotation: agent.rotation.y,
        horizon: agent.cameraHorizon,
        standing: agentMetadata.isStanding
      };

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

    function checkIfObjectVisible(objectId, metadata, inTrayectory=false) {
      let objects = metadata.objects.filter( x => x.objectId === objectId && x.visible );
      let visible = objects.length >= 1;
      if (visible) {
        if (inTrayectory) {
          outputData.visible_during_trayectory = true
        }
        else {
          outputData.success = true
        }

      }
      return visible;
    }

    function Move(metadata) {
      outputData.trayectory.push(metadata.agents[0].agent.position);
      checkIfObjectVisible(objectId, metadata.agents[0], true);
    }

    function RegisterAgentPosition(metadata) {
      let agentMetadata = metadata.agents[0];
      let agent = agentMetadata.agent;
      outputData['agent_start_location'] =
        {
          "x": agent.position.x,
          "y": agent.position.y,
          "z": agent.position.z,
          "rotation": agent.rotation.y,
          "horizon": agent.cameraHorizon,
          "standing": agentMetadata.isStanding
        };
    }

    function Rotate(metadata) {
      let agentMetadata = metadata.agents[0];
      checkIfObjectVisible(objectId, agentMetadata, true);
    }

    function InitScene(metadata) {
      console.log("--- ", metadata.agents[0].sceneName);
      outputData['scene'] = metadata.agents[0].sceneName;
      outputData['trayectory'] = [];
      outputData['actions'] = [];

      RegisterAgentPosition(metadata);


      gameInstance.SendMessage('FPSController', 'Step',JSON.stringify({
        action: 'Initialize',
        gridSize: 0.25,
        fieldOfView: 42.5,
        rotateStepDegrees: angleStepDegrees,
        agentType: 'stochastic',
        snapToGrid: false,
        continuous: true
      }));

      let teleportObject = {
        action: "TeleportFull",
        horizon: 0
      };
      let teleport = false;
      if ('x' in getParams || 'y' in getParams || 'z' in getParams) {
        teleportObject = {...teleportObject, ...agentPosition};
        teleport = true;
      }

      if ('x_rot' in getParams || 'y_rot' in getParams || 'z_rot' in getParams) {
        teleportObject.rotation = agentRotation;
        teleport = true;
      }

      console.log("-------- Teleport ", teleportObject)

      if (teleport) {
        gameInstance.SendMessage('FPSController', 'Step', JSON.stringify(
          teleportObject
        ));
      }



      // if ('x' in getParams || 'y' in getParams || 'z' in getParams) {



    }

    function TrackObjectId(metadata) {
      let agentMetadata = metadata.agents[0];
      let agent = agentMetadata.agent;
      if (agentMetadata.lastActionSuccess && agentMetadata.actionReturn ) {
        if (agentMetadata.actionReturn.length == 1) {
          objectId = agentMetadata.actionReturn[0];
        }
        else {
          $("#last-action-sub-text").html(`<strong class='red-text'>Error: Multiple objects of type ${objectType} in scene, cannot get unique id</strong>`).show();
        }
      }
      else {
        $("#last-action-sub-text").html(`<strong class='red-text'>Error: No objects of type ${objectType} in scene, cannot get unique id</strong>`).show();
      }
      // objectId =
      // console.log("------------ GOT ", agentMetadata.actionReturn, " Succ ", agentMetadata.lastActionSuccess);
    }

    function InitializeCallback(metadata) {
      console.log("------------- Initialize");
      console.log("!!!!!!!!!!!Teleport to ", agentPosition, " rotation ",agentRotation);


      // }


    }

    function TeleportFullCallback(metadata) {
      $("#reset-hit").click((e) => {
        $("#reset-hit").blur();
        outputData.reset_count += 1;
        gameInstance.SendMessage ('PhysicsSceneManager', 'SwitchScene', outputData.scene);
      }).attr("disabled", false);

      gameInstance.SendMessage('FPSController', 'Step', JSON.stringify({
        action: "ObjectTypeToObjectIds",
        objectType: objectType
      }));
    }

    let eventHandlers = {
      [null]: InitScene,
      Initialize: InitializeCallback,
      MoveAhead: Move,
      MoveBack: Move,
      RotateLeft: Rotate,
      RotateRight: Rotate,
      ObjectTypeToObjectIds: TrackObjectId,
      TeleportFull: TeleportFullCallback
    };

    function handleEvent(metadata) {
      let action = metadata.agents[0].lastAction;
      let handler = eventHandlers[action];
      if (handler !== undefined) {
        handler(metadata);
      }
      let agentMetadata = metadata.agents[0];
      let agent = agentMetadata.agent;

      if (agentMetadata.lastActionSuccess) {
        $("#last-action-text").html(`Action Success: <strong class="green-text">${agentMetadata.lastAction}</strong>`).show();
        $("#last-action-sub-text").hide();
      }
      else {
        $("#last-action-text").html(`Action Failed: <strong class="red-text">${agentMetadata.lastAction}</strong>`).show();
        $("#last-action-sub-text").html(`${agentMetadata.errorMessage}`).show();

        // console.log(agentMetadata)
      }
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
    }


    function initGame(url) {
      const t0 = performance.now();
      gameInstance = UnityLoader.instantiate("gameContainer", url, {
        onProgress: UnityProgress, Module: {
          onRuntimeInitialized: function () {
            // At this point unity loaded successfully
            UnityProgress(gameInstance, "complete");
            const t1 = performance.now();
            console.log(`Load finished. Took: ${(t1 - t0) / 1000}s`);
          },
        }
      });
    }


    // Unity Loader Script
    $.getScript(`${window.game_build}/Build/UnityLoader.js` )
      .done(function( script, textStatus ) {

        console.log("Status: ", textStatus);

        // $('#role-str').html(('role' in getParams ? getParams['role'] : 'hider').toUpperCase());
        $("#mturk_form").attr("action", isTurkSanbox ? turkSandboxUrl : turkUrl);

        let objectHtml = `<strong class="important-text">${getParams['object']}</strong>`;
        $("#instruction-text").html(`You have to move to a ${objectHtml}`);
        $("#instruction-2").html(`Move and turn around in the room, and look for a ${objectHtml}.`);
        $("#instruction-3").html(`When you have found the ${objectHtml} and you are looking at it, press the <strong class="green-text">Finish</strong> button above.`);
        $("#instruction-4").html(`Press <strong class="red-text">Reset</strong> to start over.`);
        $("#instructions-hider").show();

        $("#finish-hit").click((e) => {
          outputData['success'] = false;
          submitHit(lastMetadadta);

        }).text("Finish");

        // $(document).keypress(function(event) {
        //   if (event.keyCode === 32) {
        //     if(event.shiftKey && !hasObject){
        //       pickupFailTimeout = setTimeout(() => {
        //         $("#last-action-text").html(`Action Failed: <strong class="red-text">Pick Up Failed</strong>`).show();
        //       }, 800)
        //     } else {
        //
        //     }
        //   }
        // });
        setTimeout(() => {
          $("#finish-hit").attr("disabled", false);
        }, 'finishEnableSeconds' in getParams ? parseInt(getParams['finishEnableSeconds']) * 1000 : 10000);

        initGame(window.game_url);

      })
      .fail(function( jqxhr, settings, exception ) {
        console.error( "Triggered ajaxError handler.", exception);
      });
  }
);