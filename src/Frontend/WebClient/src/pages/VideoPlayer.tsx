import React, { useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import Hls from 'hls.js';

const VideoPlayer: React.FC = () => {
  const { videoId } = useParams<{ videoId: string }>();
  const videoRef = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    const video = videoRef.current;
    if (!video || !videoId) return;

    const source = `http://localhost:5000/api/stream/${videoId}/master.m3u8`;

    if (Hls.isSupported()) {
      const hls = new Hls({
        xhrSetup: (xhr, url) => {
          const token = localStorage.getItem('token');
          // Forward token to segments requests to allow viewing private objects
          if (token && url.includes('api/stream/')) {
            xhr.setRequestHeader('Authorization', `Bearer ${token}`);
          }
        }
      });
      hls.loadSource(source);
      hls.attachMedia(video);
      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        video.play().catch(() => console.log('Autoplay blocked'));
      });

      return () => {
        hls.destroy();
      };
    } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
      // Native HLS (Safari). Note: Safari does not automatically attach Bearer tokens.
      video.src = source;
      video.addEventListener('loadedmetadata', () => {
        video.play();
      });
    }
  }, [videoId]);

  return (
    <div style={{ padding: '2rem' }}>
      <h2>Video Player</h2>
      <video ref={videoRef} controls style={{ width: '100%', maxWidth: '800px', backgroundColor: '#000' }} />
    </div>
  );
};

export default VideoPlayer;
