#pragma once

#include <vector>
#include <deque>

class ClubMixer {
public:
    // DJ Culture Enums
    enum BeatBus { LEFT_BEAT, RIGHT_BEAT, CENTER_BEAT };
    enum ScratchCurve { LINEAR_SCRATCH, EXPO_SCRATCH };

    ClubMixer();
    ~ClubMixer();

    // Mixing functions
    void mix(float* left, float* right, int frames);
    void applyOutputDSP(float* left, float* right, int frames);

    // Control functions
    void setCrossfader(float position);
    void setVolume(int deck, float gain);
    void setMasterVolume(float gain);
    void setCrossfaderCurve(int curveType);

    // Clipping Protection Methods
    void setClippingProtectionEnabled(bool enabled) { m_clippingProtectionEnabled = enabled; }
    void setDeckVolumeCapEnabled(bool enabled) { m_deckVolumeCapEnabled = enabled; }
    void setPeakDetectionEnabled(bool enabled) { m_peakDetectionEnabled = enabled; }
    void setSoftKneeCompressorEnabled(bool enabled) { m_softKneeCompressorEnabled = enabled; }
    void setLookAheadLimiterEnabled(bool enabled) { m_lookAheadLimiterEnabled = enabled; }
    void setRmsMonitoringEnabled(bool enabled) { m_rmsMonitoringEnabled = enabled; }
    void setAutoGainReductionEnabled(bool enabled) { m_autoGainReductionEnabled = enabled; }
    void setBrickwallLimiterEnabled(bool enabled) { m_brickwallLimiterEnabled = enabled; }
    void setClippingIndicatorEnabled(bool enabled) { m_clippingIndicatorEnabled = enabled; }

    // Threshold setters
    void setClippingThreshold(float threshold) { m_clippingThreshold = threshold; }
    void setCompressorRatio(float ratio) { m_compressorRatio = ratio; }
    void setLimiterAttackTime(float attackMs) { m_limiterAttackTime = attackMs / 1000.0f; } // Convert ms to seconds
    void setLimiterReleaseTime(float releaseMs) { m_limiterReleaseTime = releaseMs / 1000.0f; }

    // Monitoring getters
    float getCurrentPeakLevel() const { return m_currentPeakLevel; }
    float getCurrentRmsLevel() const { return m_currentRmsLevel; }
    bool isClipping() const { return m_isClipping; }

    float getDeckGain(int deck);
    float getDeckVolume(int deck);
    float getMasterVolume();
    float getCrossfader() { return m_crossfader; }

private:
    float applyCurve(float value, int curveType);

    // Basic mixing parameters
    float m_crossfader;
    float m_volumes[2];
    float m_masterVolume;
    int m_curveType;

    // Clipping Protection Parameters
    bool m_clippingProtectionEnabled;
    bool m_deckVolumeCapEnabled;
    bool m_peakDetectionEnabled;
    bool m_softKneeCompressorEnabled;
    bool m_lookAheadLimiterEnabled;
    bool m_rmsMonitoringEnabled;
    bool m_autoGainReductionEnabled;
    bool m_brickwallLimiterEnabled;
    bool m_clippingIndicatorEnabled;

    // Thresholds
    float m_clippingThreshold;
    float m_compressorRatio;
    float m_limiterAttackTime;
    float m_limiterReleaseTime;

    // Monitoring state
    float m_currentPeakLevel;
    float m_currentRmsLevel;
    bool m_isClipping;

    // Look-ahead buffer for limiter
    std::deque<float> m_lookAheadBuffer;
    int m_lookAheadSamples;

    // RMS calculation
    std::vector<float> m_rmsWindow;
    int m_rmsWindowSize;

    // Auto gain reduction
    float m_autoGainReduction;

    // DSP Methods
    void applyDeckVolumeCap(float& sample, int deck);
    void updatePeakDetection(float left, float right);
    void applySoftKneeCompressor(float& left, float& right);
    void applyLookAheadLimiter(float& left, float& right);
    void updateRmsMonitoring(float left, float right);
    void applyAutoGainReduction(float& left, float& right);
    void applyBrickwallLimiter(float& left, float& right);
    void updateClippingIndicator(float left, float right);

    // Helper methods
    float softKneeCompress(float input);
    float calculateRms(const std::vector<float>& samples);
};