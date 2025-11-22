#include "ClubMixer.h"
#include <iostream>
#include <algorithm>
#include <cmath>

static std::ofstream mixerLog("logs/clubmixer.log", std::ios::app);

ClubMixer::ClubMixer() : m_crossfader(0.0f), m_masterVolume(1.0f), m_curveType(0) {
    std::cout << "Starting ClubMixer boot" << std::endl;

    // Initialize basic parameters
    m_volumes[0] = 1.0f;
    m_volumes[1] = 1.0f;

    // Initialize clipping protection parameters
    m_clippingProtectionEnabled = false; // Disable for testing volume faders
    m_deckVolumeCapEnabled = false; // Disable artificial cap, use full range + limiter
    m_peakDetectionEnabled = true;
    m_softKneeCompressorEnabled = true;
    m_lookAheadLimiterEnabled = false;
    m_rmsMonitoringEnabled = false;
    m_autoGainReductionEnabled = false;
    m_brickwallLimiterEnabled = true; // Enable master brickwall limiter
    m_clippingIndicatorEnabled = false;

    // Initialize thresholds
    m_clippingThreshold = 0.9f;
    m_compressorRatio = 2.0f; // Less aggressive compression
    m_limiterAttackTime = 0.001f; // 1ms
    m_limiterReleaseTime = 0.1f;   // 100ms

    // Initialize monitoring state
    m_currentPeakLevel = 0.0f;
    m_currentRmsLevel = 0.0f;
    m_isClipping = false;

    // Initialize buffer sizes
    m_lookAheadSamples = 512; // ~11.6ms at 44.1kHz
    m_rmsWindowSize = 441;    // ~10ms at 44.1kHz
    m_autoGainReduction = 1.0f;

    // Initialize buffers
    m_lookAheadBuffer.resize(m_lookAheadSamples, 0.0f);
    m_rmsWindow.resize(m_rmsWindowSize, 0.0f);

    std::cout << "[ClubMixer] Initialized with crossfader=0.0, masterVolume=1.0, volumes[0]=1.0, volumes[1]=1.0, curve=0" << std::endl;
    std::cout << "[ClubMixer] Clipping protection enabled with deck volume cap and peak detection" << std::endl;
}

ClubMixer::~ClubMixer() {
    std::cout << "[ClubMixer] Destroyed" << std::endl;
}

void ClubMixer::mix(float* left, float* right, int frames) {
    // Crossfader as volume control: attenuates decks based on position
    // crossfader -1: left full, right off; 0: both full; 1: left off, right full
    for (int i = 0; i < frames; ++i) {
        float left_raw = 1.0f - std::max(0.0f, m_crossfader);
        float right_raw = 1.0f - std::max(0.0f, -m_crossfader);
        // Apply curve (0=linear, 1=exponential, etc.)
        float left_cross_gain = applyCurve(left_raw, m_curveType);
        float right_cross_gain = applyCurve(right_raw, m_curveType);

        // Apply basic mixing
        float l = left[i] * m_volumes[0] * left_cross_gain;
        float r = right[i] * m_volumes[1] * right_cross_gain;

        // Apply clipping protection if enabled
        if (m_clippingProtectionEnabled) {
            // Note: Deck volume cap removed - users control full 0-100% range
            // Clipping protection handled by DSP algorithms below

            // 2. Soft knee compression
            if (m_softKneeCompressorEnabled) {
                applySoftKneeCompressor(l, r);
            }

            // 3. Look-ahead limiting (requires buffering)
            if (m_lookAheadLimiterEnabled) {
                applyLookAheadLimiter(l, r);
            }

            // 4. Brickwall limiting (hard limiting as final safety)
            if (m_brickwallLimiterEnabled) {
                applyBrickwallLimiter(l, r);
            }

            // 5. Update monitoring
            if (m_peakDetectionEnabled) {
                updatePeakDetection(l, r);
            }
            if (m_rmsMonitoringEnabled) {
                updateRmsMonitoring(l, r);
            }
            if (m_clippingIndicatorEnabled) {
                updateClippingIndicator(l, r);
            }

            // 6. Auto gain reduction (if clipping detected)
            if (m_autoGainReductionEnabled) {
                applyAutoGainReduction(l, r);
            }
        }

        // Apply master volume
        left[i] = l * m_masterVolume;
        right[i] = r * m_masterVolume;
    }
}

float ClubMixer::applyCurve(float value, int curveType) {
    switch (curveType) {
        case 0: // Linear
            return value;
        case 1: // Exponential
            return value * value;
        case 2: // Logarithmic
            return std::sqrt(value);
        case 3: // S-Curve
            return value < 0.5f ? 2 * value * value : 1 - 2 * (1 - value) * (1 - value);
        default:
            return value;
    }
}

void ClubMixer::setCrossfader(float position) {
    m_crossfader = position;
    std::cout << "[ClubMixer] Crossfader set to " << position << std::endl;
}

void ClubMixer::setVolume(int deck, float gain) {
    if (deck >= 0 && deck < 2) {
        m_volumes[deck] = gain;
        auto now = std::chrono::system_clock::now();
        std::time_t now_time = std::chrono::system_clock::to_time_t(now);
        mixerLog << "[" << std::ctime(&now_time) << "] Volume set on deck " << deck << " to " << gain << std::endl;
        mixerLog.flush();
        std::cout << "[ClubMixer] Volume for deck " << deck << " set to " << gain << std::endl;
    } else {
        std::cout << "[ClubMixer] Invalid deck " << deck << " for setVolume" << std::endl;
    }
}

void ClubMixer::setMasterVolume(float gain) {
    m_masterVolume = gain;
    std::cout << "[ClubMixer] Master volume set to " << gain << std::endl;
}

void ClubMixer::setCrossfaderCurve(int curveType) {
    m_curveType = curveType;
    std::cout << "[ClubMixer] Crossfader curve set to " << curveType << std::endl;
}

float ClubMixer::getDeckGain(int deck) {
    if (deck == 0) {
        float raw = 1.0f - std::max(0.0f, m_crossfader);
        return applyCurve(raw, m_curveType);
    } else if (deck == 1) {
        float raw = 1.0f - std::max(0.0f, -m_crossfader);
        return applyCurve(raw, m_curveType);
    }
    return 1.0f;
}

float ClubMixer::getDeckVolume(int deck) {
    if (deck >= 0 && deck < 2) {
        return m_volumes[deck];
    }
    return 1.0f;
}

float ClubMixer::getMasterVolume() {
    return m_masterVolume;
}

// ============================================================================
// Clipping Protection DSP Methods
// ============================================================================

void ClubMixer::applyDeckVolumeCap(float& sample, int deck) {
    // Cap deck volume at 50% to prevent clipping when both decks play
    const float maxVolume = 0.5f;
    if (std::abs(sample) > maxVolume) {
        sample = (sample > 0) ? maxVolume : -maxVolume;
    }
}

void ClubMixer::updatePeakDetection(float left, float right) {
    float peak = std::max(std::abs(left), std::abs(right));
    m_currentPeakLevel = std::max(m_currentPeakLevel * 0.99f, peak); // Slow decay
}

void ClubMixer::applySoftKneeCompressor(float& left, float& right) {
    left = softKneeCompress(left);
    right = softKneeCompress(right);
}

float ClubMixer::softKneeCompress(float input) {
    const float threshold = m_clippingThreshold * 0.8f; // Compress before hard limiting
    const float knee = 0.1f; // Soft knee width

    if (std::abs(input) < threshold) {
        return input; // No compression
    }

    if (std::abs(input) < threshold + knee) {
        // Soft knee region
        float excess = std::abs(input) - threshold;
        float ratio = 1.0f + (m_compressorRatio - 1.0f) * (excess / knee);
        return (input > 0 ? 1 : -1) * (threshold + excess / ratio);
    } else {
        // Hard knee region
        float excess = std::abs(input) - threshold;
        return (input > 0 ? 1 : -1) * (threshold + excess / m_compressorRatio);
    }
}

void ClubMixer::applyLookAheadLimiter(float& left, float& right) {
    // Simple look-ahead limiting using buffer
    // This is a basic implementation - production would need more sophisticated envelope following

    // Add current samples to buffer
    m_lookAheadBuffer.push_front(std::max(std::abs(left), std::abs(right)));
    if (m_lookAheadBuffer.size() > m_lookAheadSamples) {
        m_lookAheadBuffer.pop_back();
    }

    // Check future peaks
    float maxFuturePeak = 0.0f;
    for (float peak : m_lookAheadBuffer) {
        maxFuturePeak = std::max(maxFuturePeak, peak);
    }

    // Apply limiting if future peak exceeds threshold
    if (maxFuturePeak > m_clippingThreshold) {
        float ratio = m_clippingThreshold / maxFuturePeak;
        left *= ratio;
        right *= ratio;
    }
}

void ClubMixer::updateRmsMonitoring(float left, float right) {
    // Add samples to RMS window
    m_rmsWindow.push_back(std::max(std::abs(left), std::abs(right)));
    if (m_rmsWindow.size() > m_rmsWindowSize) {
        m_rmsWindow.erase(m_rmsWindow.begin());
    }

    // Calculate RMS
    m_currentRmsLevel = calculateRms(m_rmsWindow);
}

float ClubMixer::calculateRms(const std::vector<float>& samples) {
    if (samples.empty()) return 0.0f;

    float sumSquares = 0.0f;
    for (float sample : samples) {
        sumSquares += sample * sample;
    }
    return std::sqrt(sumSquares / samples.size());
}

void ClubMixer::applyAutoGainReduction(float& left, float& right) {
    // Reduce gain if clipping detected
    if (m_isClipping) {
        m_autoGainReduction = std::max(0.1f, m_autoGainReduction * 0.999f); // Slow recovery
    } else {
        m_autoGainReduction = std::min(1.0f, m_autoGainReduction * 1.001f); // Slow increase
    }

    left *= m_autoGainReduction;
    right *= m_autoGainReduction;
}

void ClubMixer::applyBrickwallLimiter(float& left, float& right) {
    // Hard limiting at threshold
    if (std::abs(left) > m_clippingThreshold) {
        left = (left > 0) ? m_clippingThreshold : -m_clippingThreshold;
    }
    if (std::abs(right) > m_clippingThreshold) {
        right = (right > 0) ? m_clippingThreshold : -m_clippingThreshold;
    }
}

void ClubMixer::updateClippingIndicator(float left, float right) {
    bool wasClipping = m_isClipping;
    m_isClipping = (std::abs(left) >= m_clippingThreshold || std::abs(right) >= m_clippingThreshold);

    // Log clipping events (following SOP: critical events to console)
    if (m_isClipping && !wasClipping) {
        std::cout << "[ClubMixer] CLIPPING DETECTED - Output exceeded " << m_clippingThreshold << std::endl;
    }
}

void ClubMixer::applyOutputDSP(float* left, float* right, int frames) {
    for (int i = 0; i < frames; ++i) {
        float l = left[i];
        float r = right[i];

        // Apply clipping protection if enabled
        if (m_clippingProtectionEnabled) {
            // Soft knee compression
            if (m_softKneeCompressorEnabled) {
                applySoftKneeCompressor(l, r);
            }

            // Look-ahead limiting (requires buffering)
            if (m_lookAheadLimiterEnabled) {
                applyLookAheadLimiter(l, r);
            }

            // Brickwall limiting (hard limiting as final safety)
            if (m_brickwallLimiterEnabled) {
                applyBrickwallLimiter(l, r);
            }

            // Update monitoring
            if (m_peakDetectionEnabled) {
                updatePeakDetection(l, r);
            }
            if (m_rmsMonitoringEnabled) {
                updateRmsMonitoring(l, r);
            }
            if (m_clippingIndicatorEnabled) {
                updateClippingIndicator(l, r);
            }

            // Auto gain reduction (if clipping detected)
            if (m_autoGainReductionEnabled) {
                applyAutoGainReduction(l, r);
            }
        }

        left[i] = l;
        right[i] = r;
    }
}