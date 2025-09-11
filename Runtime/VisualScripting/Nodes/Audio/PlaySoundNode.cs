using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting.Execution;

namespace FluxFramework.VisualScripting.Nodes
{
    /// <summary>
    /// A node that controls an AudioSource to play, stop, or pause audio clips.
    /// It can wait for a clip to finish before continuing the execution flow.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaySoundNode", menuName = "Flux/Visual Scripting/Audio/Play Sound")]
    public class PlaySoundNode : FluxNodeBase
    {
        [Tooltip("The default audio clip to play if none is provided via the input port.")]
        [SerializeField] private AudioClip _audioClip;
        
        [Tooltip("The default volume for the audio clip.")]
        [SerializeField] [Range(0f, 1f)] private float _volume = 1f;

        [Tooltip("If true, the audio clip will loop.")]
        [SerializeField] private bool _loop = false;
        
        // This dictionary will store the active 'wait for completion' coroutine for each runner instance.
        private readonly Dictionary<GameObject, Coroutine> _completionCoroutines = new Dictionary<GameObject, Coroutine>();

        public override string NodeName => "Play Sound";
        public override string Category => "Audio";
        
        protected override void InitializePorts()
        {
            // Execution inputs
            AddInputPort("play", "▶ Play", FluxPortType.Execution, "void", false);
            AddInputPort("stop", "▶ Stop", FluxPortType.Execution, "void", false);
            
            // Data inputs
            AddInputPort("audioSource", "Audio Source", FluxPortType.Data, "AudioSource", true, null, "The AudioSource to use. If null, a temporary one will be created on the runner.");
            AddInputPort("audioClip", "Clip", FluxPortType.Data, "AudioClip", false, _audioClip);
            AddInputPort("volume", "Volume", FluxPortType.Data, "float", false, _volume);
            AddInputPort("loop", "Loop", FluxPortType.Data, "bool", false, _loop);
            
            // Execution outputs
            AddOutputPort("onPlay", "▶ On Play", FluxPortType.Execution, "void", false);
            AddOutputPort("onStop", "▶ On Stop", FluxPortType.Execution, "void", false);
            AddOutputPort("onComplete", "▶ On Complete", FluxPortType.Execution, "void", false);
            
            // Data outputs
            AddOutputPort("isPlaying", "Is Playing", FluxPortType.Data, "bool", false);
        }

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            AudioSource source = GetInputValue<AudioSource>(inputs, "audioSource");

            if (inputs.ContainsKey("play"))
            {
                PlaySound(executor, inputs, outputs);
            }
            if (inputs.ContainsKey("stop"))
            {
                StopSound(source, outputs);
            }
            
            // Update data outputs
            SetOutputValue(outputs, "isPlaying", source != null && source.isPlaying);
        }

        private void PlaySound(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {
            AudioClip clip = GetInputValue<AudioClip>(inputs, "audioClip", _audioClip);
            AudioSource source = GetInputValue<AudioSource>(inputs, "audioSource");
            float volume = GetInputValue<float>(inputs, "volume", _volume);
            bool loop = GetInputValue<bool>(inputs, "loop", _loop);

            if (clip == null)
            {
                Debug.LogWarning("PlaySoundNode: No AudioClip provided.", this);
                return;
            }

            var context = executor.Runner.GetContextObject();
            if (context == null)
            {
                Debug.LogError("PlaySoundNode: A context GameObject (runner) is required to play sound.", this);
                return;
            }

            // If no AudioSource is provided, create a temporary one on the context GameObject.
            bool isTemporarySource = false;
            if (source == null)
            {
                source = context.AddComponent<AudioSource>();
                isTemporarySource = true;
            }
            
            // Stop any previously running completion coroutine for this context.
            if (_completionCoroutines.TryGetValue(context, out Coroutine existingCoroutine))
            {
                if(existingCoroutine != null) context.GetComponent<MonoBehaviour>()?.StopCoroutine(existingCoroutine);
                _completionCoroutines.Remove(context);
            }

            source.clip = clip;
            source.volume = volume;
            source.loop = loop;
            source.Play();

            SetOutputValue(outputs, "onPlay", null);

            // If not looping, start a coroutine to wait for completion.
            if (!loop)
            {
                var coroutine = context.GetComponent<MonoBehaviour>()?.StartCoroutine(WaitForSoundCompletion(executor, source, isTemporarySource));
                if(coroutine != null) _completionCoroutines[context] = coroutine;
            }
        }

        private void StopSound(AudioSource source, Dictionary<string, object> outputs)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
                SetOutputValue(outputs, "onStop", null);
            }
        }

        private IEnumerator WaitForSoundCompletion(FluxGraphExecutor executor, AudioSource source, bool isTemporary)
        {
            // Wait until the audio clip has finished playing.
            // We add a small buffer to ensure the isPlaying flag has updated.
            yield return new WaitWhile(() => source.isPlaying);

            // Trigger the onComplete execution port.
            executor.ContinueFromPort(this, "onComplete", new Dictionary<string, object>());

            // If the source was temporary, destroy it after a short delay.
            if (isTemporary && source != null)
            {
                yield return new WaitForSeconds(0.1f);
                Destroy(source);
            }

            // Clean up the coroutine reference.
            var context = executor.Runner.GetContextObject();
            if(context != null) _completionCoroutines.Remove(context);
        }

        // Clean up any running coroutines if the graph asset is destroyed
        protected void OnDestroy()
        {
            // Note: This is a best-effort cleanup. The GraphSubscriptionCleanup on the runner
            // is the primary mechanism for robust instance-based cleanup.
            _completionCoroutines.Clear();
        }
    }
}