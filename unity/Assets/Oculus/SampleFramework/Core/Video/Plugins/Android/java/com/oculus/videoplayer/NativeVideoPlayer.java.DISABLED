/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

************************************************************************************/

package com.oculus.videoplayer;

import android.content.Context;
import android.graphics.SurfaceTexture;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import android.os.storage.OnObbStateChangeListener;
import android.os.storage.StorageManager;
//import android.support.annotation.Nullable;
import android.util.Log;
import android.view.Surface;


import com.google.android.exoplayer2.C;
import com.google.android.exoplayer2.C.ContentType;
import com.google.android.exoplayer2.DefaultRenderersFactory;
import com.google.android.exoplayer2.ExoPlaybackException;
import com.google.android.exoplayer2.ExoPlayer;
import com.google.android.exoplayer2.ExoPlayerFactory;
import com.google.android.exoplayer2.Format;
import com.google.android.exoplayer2.PlaybackParameters;
import com.google.android.exoplayer2.PlaybackPreparer;
import com.google.android.exoplayer2.Player;
import com.google.android.exoplayer2.Renderer;
import com.google.android.exoplayer2.RenderersFactory;
import com.google.android.exoplayer2.SimpleExoPlayer;
import com.google.android.exoplayer2.Timeline;
import com.google.android.exoplayer2.audio.AudioProcessor;
import com.google.android.exoplayer2.audio.AudioRendererEventListener;
import com.google.android.exoplayer2.audio.AudioSink;
import com.google.android.exoplayer2.drm.DefaultDrmSessionManager;
import com.google.android.exoplayer2.drm.DrmSessionManager;
import com.google.android.exoplayer2.drm.FrameworkMediaCrypto;
import com.google.android.exoplayer2.drm.FrameworkMediaDrm;
import com.google.android.exoplayer2.drm.HttpMediaDrmCallback;
import com.google.android.exoplayer2.drm.UnsupportedDrmException;
import com.google.android.exoplayer2.mediacodec.MediaCodecRenderer.DecoderInitializationException;
import com.google.android.exoplayer2.mediacodec.MediaCodecSelector;
import com.google.android.exoplayer2.mediacodec.MediaCodecUtil.DecoderQueryException;
import com.google.android.exoplayer2.metadata.MetadataOutput;
import com.google.android.exoplayer2.source.BehindLiveWindowException;
import com.google.android.exoplayer2.source.ConcatenatingMediaSource;
import com.google.android.exoplayer2.source.ExtractorMediaSource;
import com.google.android.exoplayer2.source.MediaSource;
import com.google.android.exoplayer2.source.TrackGroupArray;
import com.google.android.exoplayer2.source.ads.AdsLoader;
import com.google.android.exoplayer2.source.ads.AdsMediaSource;
import com.google.android.exoplayer2.source.dash.DashChunkSource;
import com.google.android.exoplayer2.source.dash.DefaultDashChunkSource;
import com.google.android.exoplayer2.source.dash.DashMediaSource;
import com.google.android.exoplayer2.source.hls.HlsMediaSource;
import com.google.android.exoplayer2.source.smoothstreaming.DefaultSsChunkSource;
import com.google.android.exoplayer2.source.smoothstreaming.SsChunkSource;
import com.google.android.exoplayer2.source.smoothstreaming.SsMediaSource;
import com.google.android.exoplayer2.text.TextOutput;
import com.google.android.exoplayer2.trackselection.AdaptiveTrackSelection;
import com.google.android.exoplayer2.trackselection.DefaultTrackSelector;
import com.google.android.exoplayer2.trackselection.MappingTrackSelector.MappedTrackInfo;
import com.google.android.exoplayer2.trackselection.RandomTrackSelection;
import com.google.android.exoplayer2.trackselection.TrackSelection;
import com.google.android.exoplayer2.trackselection.TrackSelectionArray;
import com.google.android.exoplayer2.upstream.BandwidthMeter;
import com.google.android.exoplayer2.upstream.DataSource;
import com.google.android.exoplayer2.upstream.DefaultBandwidthMeter;
import com.google.android.exoplayer2.upstream.DefaultDataSourceFactory;
import com.google.android.exoplayer2.upstream.DefaultHttpDataSourceFactory;
import com.google.android.exoplayer2.upstream.FileDataSourceFactory;
import com.google.android.exoplayer2.upstream.HttpDataSource;
import com.google.android.exoplayer2.upstream.cache.Cache;
import com.google.android.exoplayer2.upstream.cache.CacheDataSource;
import com.google.android.exoplayer2.upstream.cache.CacheDataSourceFactory;
import com.google.android.exoplayer2.upstream.cache.NoOpCacheEvictor;
import com.google.android.exoplayer2.upstream.cache.SimpleCache;
import com.google.android.exoplayer2.util.ErrorMessageProvider;
import com.google.android.exoplayer2.util.EventLogger;
import com.google.android.exoplayer2.util.Util;
import com.google.android.exoplayer2.video.MediaCodecVideoRenderer;
import com.google.android.exoplayer2.video.VideoListener;
import com.google.android.exoplayer2.video.VideoRendererEventListener;
import com.twobigears.audio360.AudioEngine;
import com.twobigears.audio360.ChannelMap;
import com.twobigears.audio360.SpatDecoderQueue;
import com.twobigears.audio360.TBQuat;
import com.twobigears.audio360exo2.Audio360Sink;
import com.twobigears.audio360exo2.OpusRenderer;
import java.io.File;
import java.lang.reflect.Constructor;
import java.lang.Math;
import java.lang.System;
import java.net.CookieHandler;
import java.net.CookieManager;
import java.net.CookiePolicy;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;

/**
 * Created by trevordasch on 9/19/2018.
 */

public class NativeVideoPlayer
{
    private static final String TAG = "NativeVideoPlayer"
    ;
    static AudioEngine engine;
    static SpatDecoderQueue spat;
    static final float SAMPLE_RATE = 48000.f;    
    static final int BUFFER_SIZE = 1024;
    static final int QUEUE_SIZE_IN_SAMPLES = 40960;

    static SimpleExoPlayer exoPlayer;
    static AudioSink audio360Sink;
    static File downloadDirectory;
    static Cache downloadCache;
    static FrameworkMediaDrm mediaDrm;

    static Handler handler;

    static volatile boolean isPlaying;
    static volatile int currentPlaybackState;
    static volatile int stereoMode = -1;
    static volatile int width;
    static volatile int height;

    static volatile long duration;

    static volatile long lastPlaybackPosition;
    static volatile long lastPlaybackUpdateTime;
    static volatile float lastPlaybackSpeed;

    private static void updatePlaybackState() {
        duration = exoPlayer.getDuration();
        lastPlaybackPosition = exoPlayer.getCurrentPosition();
        lastPlaybackSpeed = isPlaying ? exoPlayer.getPlaybackParameters().speed : 0;
        lastPlaybackUpdateTime = System.currentTimeMillis();
        Format format = exoPlayer.getVideoFormat();
        if (format != null) {
            stereoMode = format.stereoMode;
            width = format.width;
            height = format.height;
        }
        else {
            stereoMode = -1;
            width = 0;
            height = 0;
        }
    }

    private static Handler getHandler()
    {
        if (handler == null)
        {
            handler = new Handler(Looper.getMainLooper());
        }

        return handler;
    }

    private static class CustomRenderersFactory extends DefaultRenderersFactory {
        private Context myContext;
        private long myAllowedVideoJoiningTimeMs = 5000;
        public CustomRenderersFactory(Context context) {
            super(context);
            this.myContext = context;
        }

        @Override
        public DefaultRenderersFactory setAllowedVideoJoiningTimeMs(long allowedVideoJoiningTimeMs) {
            super.setAllowedVideoJoiningTimeMs(allowedVideoJoiningTimeMs);
            myAllowedVideoJoiningTimeMs = allowedVideoJoiningTimeMs;
            return this;
          }

        @Override
        public Renderer[] createRenderers(
            Handler eventHandler,
            VideoRendererEventListener videoRendererEventListener,
            AudioRendererEventListener audioRendererEventListener,
            TextOutput textRendererOutput,
            MetadataOutput metadataRendererOutput,
            /*@Nullable*/ DrmSessionManager<FrameworkMediaCrypto> drmSessionManager) {

            Renderer[] renderers = super.createRenderers(
                eventHandler, 
                videoRendererEventListener, 
                audioRendererEventListener,
                textRendererOutput,
                metadataRendererOutput,
                drmSessionManager);

            ArrayList<Renderer> rendererList = new ArrayList<Renderer>(Arrays.asList(renderers));
            
            // The output latency of the engine can be used to compensate for sync
            double latency = engine.getOutputLatencyMs();

            // Audio: opus codec with the spatial audio engine
            // TBE_8_2 implies 10 channels of audio (8 channels of spatial audio, 2 channels of head-locked)
            audio360Sink = new Audio360Sink(spat, ChannelMap.TBE_8_2, latency);
            final OpusRenderer audioRenderer = new OpusRenderer(audio360Sink);

            rendererList.add(audioRenderer);

            renderers = rendererList.toArray(renderers);
            return renderers;
        }
    }

    private static File getDownloadDirectory(Context context) {
        if (downloadDirectory == null) {
            downloadDirectory = context.getExternalFilesDir(null);
            if (downloadDirectory == null) {
                downloadDirectory = context.getFilesDir();
            }
        }
        return downloadDirectory;
    }

    private static synchronized Cache getDownloadCache(Context context) {
        if (downloadCache == null) {
            File downloadContentDirectory = new File(getDownloadDirectory(context), "downloads");
            downloadCache = new SimpleCache(downloadContentDirectory, new NoOpCacheEvictor());
        }
        return downloadCache;
      }

    private static CacheDataSourceFactory buildReadOnlyCacheDataSource(
      DefaultDataSourceFactory upstreamFactory, Cache cache) {
    return new CacheDataSourceFactory(
        cache,
        upstreamFactory,
        new FileDataSourceFactory(),
        /* cacheWriteDataSinkFactory= */ null,
        CacheDataSource.FLAG_IGNORE_CACHE_ON_ERROR,
        /* eventListener= */ null);
  }

  /** Returns a {@link DataSource.Factory}. */
  public static DataSource.Factory buildDataSourceFactory(Context context) {
    DefaultDataSourceFactory upstreamFactory =
        new DefaultDataSourceFactory(context, null, buildHttpDataSourceFactory(context));
    return buildReadOnlyCacheDataSource(upstreamFactory, getDownloadCache(context));
  }

    /** Returns a {@link HttpDataSource.Factory}. */
    public static HttpDataSource.Factory buildHttpDataSourceFactory(Context context) {
        return new DefaultHttpDataSourceFactory(Util.getUserAgent(context, "NativeVideoPlayer"));
    }

    private static DefaultDrmSessionManager<FrameworkMediaCrypto> buildDrmSessionManagerV18(Context context,
        UUID uuid, String licenseUrl, String[] keyRequestPropertiesArray, boolean multiSession)
        throws UnsupportedDrmException {
        HttpDataSource.Factory licenseDataSourceFactory = buildHttpDataSourceFactory(context);
        HttpMediaDrmCallback drmCallback =
            new HttpMediaDrmCallback(licenseUrl, licenseDataSourceFactory);
        if (keyRequestPropertiesArray != null) {
            for (int i = 0; i < keyRequestPropertiesArray.length - 1; i += 2) {
                drmCallback.setKeyRequestProperty(keyRequestPropertiesArray[i],
                keyRequestPropertiesArray[i + 1]);
            }
        }
        if (mediaDrm != null)
        {
            mediaDrm.release();
        }
        mediaDrm = FrameworkMediaDrm.newInstance(uuid);
        return new DefaultDrmSessionManager<>(uuid, mediaDrm, drmCallback, null, multiSession);
    }

    @SuppressWarnings("unchecked")
    private static MediaSource buildMediaSource(Context context, Uri uri, /*@Nullable*/ String overrideExtension, DataSource.Factory dataSourceFactory) {
        @ContentType int type = Util.inferContentType(uri, overrideExtension);
        switch (type) {
            case C.TYPE_DASH:

                return new DashMediaSource.Factory(new DefaultDashChunkSource.Factory(dataSourceFactory), dataSourceFactory)
                .createMediaSource(uri);
            case C.TYPE_SS:
                return new SsMediaSource.Factory(new DefaultSsChunkSource.Factory(dataSourceFactory), dataSourceFactory)
                .createMediaSource(uri);
            case C.TYPE_HLS:
                return new HlsMediaSource.Factory(dataSourceFactory)
                .createMediaSource(uri);
            case C.TYPE_OTHER:
                return new ExtractorMediaSource.Factory(dataSourceFactory).createMediaSource(uri);
            default: {
                throw new IllegalStateException("Unsupported type: " + type);
            }
        }
    }

    public static void playVideo( final Context context, final String filePath, final String drmLicenseUrl, final Surface surface)
    {
        // set up exoplayer on main thread
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                // 1. Create a default TrackSelector
                BandwidthMeter bandwidthMeter = new DefaultBandwidthMeter();
                TrackSelection.Factory videoTrackSelectionFactory =
                        new AdaptiveTrackSelection.Factory(bandwidthMeter);
                DefaultTrackSelector trackSelector =
                        new DefaultTrackSelector(videoTrackSelectionFactory);
                // Produces DataSource instances through which media data is loaded.
                DataSource.Factory dataSourceFactory = buildDataSourceFactory(context);

                Uri uri = Uri.parse( filePath );

                if (filePath.startsWith( "jar:file:" )) {
                    if (filePath.contains(".apk")) { // APK
                        uri = new Uri.Builder().scheme( "asset" ).path( filePath.substring( filePath.indexOf( "/assets/" ) + "/assets/".length() ) ).build();
                    }
                    else if (filePath.contains(".obb")) { // OBB
                        String obbPath = filePath.substring(11, filePath.indexOf(".obb") + 4);

                        StorageManager sm = (StorageManager)context.getSystemService(Context.STORAGE_SERVICE);
                        if (!sm.isObbMounted(obbPath))
                        {
                            sm.mountObb(obbPath, null, new OnObbStateChangeListener() {
                                @Override
                                public void onObbStateChange(String path, int state) {
                                    super.onObbStateChange(path, state);
                                }
                            });
                        }

                        uri = new Uri.Builder().scheme( "file" ).path( sm.getMountedObbPath(obbPath) + filePath.substring(filePath.indexOf(".obb") + 5) ).build();
                    }
                }

                // Set up video source if drmLicenseUrl is set
                DefaultDrmSessionManager<FrameworkMediaCrypto> drmSessionManager = null;
                if (drmLicenseUrl != null && drmLicenseUrl.length() > 0) {
                    try {
                        drmSessionManager = buildDrmSessionManagerV18(context,
                      Util.getDrmUuid("widevine"), drmLicenseUrl, null, false);
                    } catch (UnsupportedDrmException e) {
                        Log.e(TAG, "Unsupported DRM!", e);
                    }
                }

                // This is the MediaSource representing the media to be played.
                MediaSource videoSource = buildMediaSource(context, uri, null, dataSourceFactory);

                Log.d(TAG, "Requested play of " +filePath + " uri: "+uri.toString());

                // 2. Create the player
                //--------------------------------------
                //- Audio Engine
                if (engine == null) 
                {
                    engine = AudioEngine.create(SAMPLE_RATE, BUFFER_SIZE, QUEUE_SIZE_IN_SAMPLES, context);
                    spat = engine.createSpatDecoderQueue();
                    engine.start();
                }

                //--------------------------------------
                //- ExoPlayer

                // Create our modified ExoPlayer instance
                if (exoPlayer != null)
                {
                    exoPlayer.release();
                }
                exoPlayer = ExoPlayerFactory.newSimpleInstance(context, new CustomRenderersFactory(context), trackSelector, drmSessionManager);


                exoPlayer.addListener(new Player.DefaultEventListener() {

                    @Override
                    public void onPlayerStateChanged(boolean playWhenReady, int playbackState)
                    {
                        isPlaying = playWhenReady && (playbackState == Player.STATE_READY || playbackState == Player.STATE_BUFFERING);
                        currentPlaybackState = playbackState;

                        updatePlaybackState();
                    }

                    @Override 
                    public void onPlaybackParametersChanged(PlaybackParameters params)
                    {
                        updatePlaybackState();
                    }

                    @Override
                    public void onPositionDiscontinuity(int reason)
                    {
                        updatePlaybackState();
                    }

                });            

                exoPlayer.setVideoSurface( surface );

                // Prepare the player with the source.
                exoPlayer.prepare(videoSource);


                exoPlayer.setPlayWhenReady( true );

            }
        });
    }

    public static void setLooping(final boolean looping)
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    if ( looping )
                    {
                        exoPlayer.setRepeatMode( Player.REPEAT_MODE_ONE );
                    }
                    else 
                    {
                        exoPlayer.setRepeatMode( Player.REPEAT_MODE_OFF );
                    }
                }
            }
        });
    }

    public static void stop()
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    exoPlayer.stop();
                    exoPlayer.release();
                    exoPlayer = null;
                }
                if ( mediaDrm != null) {
                    mediaDrm.release();
                    mediaDrm = null;
                }
                if (engine != null)
                {
                    engine.destroySpatDecoderQueue(spat);
                    engine.delete();
                    spat = null;
                    engine = null;
                }

            }
        });
    }

    public static void pause()
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    exoPlayer.setPlayWhenReady(false);
                }
            }
        });
    }

    public static void resume()
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    exoPlayer.setPlayWhenReady(true);
                }
            }
        });
    }

    public static void setPlaybackSpeed(final float speed)
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    PlaybackParameters param = new PlaybackParameters(speed);
                    exoPlayer.setPlaybackParameters(param);
                }
            }
        });
    
    }

    public static void setListenerRotationQuaternion(float x, float y, float z, float w)
    {
        if (engine != null)
        {
            engine.setListenerRotation(new TBQuat(x,y,z,w));
        }
    }

    public static boolean getIsPlaying()
    {
        return isPlaying;
    }

    public static int getCurrentPlaybackState()
    {
        return currentPlaybackState;
    }

    public static long getDuration()
    {
        return duration;
    }

    public static int getStereoMode()
    {
        return stereoMode;
    }

    public static int getWidth() 
    {
        return width;
    }

    public static int getHeight()
    {
        return height;
    }

    public static long getPlaybackPosition()
    {
        return Math.max(0, Math.min(duration, lastPlaybackPosition + (long)((System.currentTimeMillis() - lastPlaybackUpdateTime) * lastPlaybackSpeed)));
    }

    public static void setPlaybackPosition(final long position)
    {
        getHandler().post( new Runnable()
        {
            @Override
            public void run()
            {
                if ( exoPlayer != null )
                {
                    Timeline timeline = exoPlayer.getCurrentTimeline();
                    if ( timeline != null ) 
                    {
                        int windowIndex = timeline.getFirstWindowIndex(false);
                        long windowPositionUs = position * 1000L;
                        Timeline.Window tmpWindow = new Timeline.Window();
                        for(int i = timeline.getFirstWindowIndex(false);
                            i < timeline.getLastWindowIndex(false); i++)
                        {
                            timeline.getWindow(i, tmpWindow);

                            if (tmpWindow.durationUs > windowPositionUs) 
                            {
                                break;
                            }

                            windowIndex++;
                            windowPositionUs -= tmpWindow.durationUs;
                        }

                        exoPlayer.seekTo(windowIndex, windowPositionUs / 1000L);
                    }
                }
            }
        });
    }
}
