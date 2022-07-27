// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

// TODO move to editor after fixing multiple assemblies problem
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

