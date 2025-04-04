<template>
  <div class="request-flow-visualization">
    <div class="flow-container">
      <div class="client-node node">
        <div class="node-icon">
          <i data-feather="monitor"></i>
        </div>
        <div class="node-label">Client</div>
      </div>
      
      <div class="flow-arrow" :class="{ 'animated': animationEnabled }">
        <div class="arrow-line"></div>
        <div class="arrow-head"></div>
      </div>
      
      <div class="interceptor-node node">
        <div class="node-icon">
          <i data-feather="filter"></i>
        </div>
        <div class="node-label">Interceptor</div>
      </div>
      
      <!-- Se matchedRule esiste, mostra un percorso alternativo che passa attraverso la regola -->
      <template v-if="matchedRule">
        <div class="flow-arrow rule-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
        
        <div class="rule-node node">
          <div class="node-icon">
            <i data-feather="book"></i>
          </div>
          <div class="node-label">Rule: {{ matchedRule.name }}</div>
        </div>
        
        <div class="flow-arrow return-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
      </template>
      
      <!-- Se non c'è una regola abbinata e isProxied è true, mostra il flusso proxy -->
      <template v-else-if="isProxied">
        <div class="flow-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
        
        <div class="proxy-node node">
          <div class="node-icon">
            <i data-feather="server"></i>
          </div>
          <div class="node-label">Proxy</div>
        </div>
        
        <div class="flow-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
        
        <div class="target-node node">
          <div class="node-icon">
            <i data-feather="globe"></i>
          </div>
          <div class="node-label">Target: {{ targetHost }}</div>
        </div>
        
        <div class="flow-arrow return-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
      </template>
      
      <!-- Se non c'è una regola abbinata e non è proxied, mostra una risposta diretta -->
      <template v-else>
        <div class="flow-arrow return-arrow" :class="{ 'animated': animationEnabled }">
          <div class="arrow-line"></div>
          <div class="arrow-head"></div>
        </div>
      </template>
    </div>
    
    <div class="flow-legend">
      <div class="legend-item">
        <span class="arrow-sample"></span> Request flow
      </div>
      <div class="legend-item">
        <span class="arrow-sample return"></span> Response flow
      </div>
      <div class="legend-item" v-if="matchedRule">
        <span class="arrow-sample rule"></span> Rule matched
      </div>
    </div>
  </div>
</template>

<script>
import { gsap } from 'gsap'

export default {
  name: 'RequestFlowVisualization',
  
  props: {
    request: {
      type: Object,
      required: true
    },
    matchedRule: {
      type: Object,
      default: null
    }
  },
  
  data() {
    return {
      animationEnabled: true
    }
  },
  
  computed: {
    isProxied() {
      return this.request && this.request.isProxied
    },
    
    targetHost() {
      if (!this.request || !this.request.url) return ''
      
      try {
        const url = new URL(this.request.url)
        return url.hostname
      } catch (e) {
        return ''
      }
    }
  },
  
  mounted() {
    this.initializeAnimations()
    feather.replace()
  },
  
  updated() {
    feather.replace()
  },
  
  methods: {
    initializeAnimations() {
      // Animare le frecce con GSAP
      if (this.animationEnabled) {
        this.$nextTick(() => {
          const arrowElements = this.$el.querySelectorAll('.flow-arrow.animated .arrow-line')
          
          arrowElements.forEach(el => {
            const directionMultiplier = el.parentElement.classList.contains('return-arrow') ? -1 : 1
            
            gsap.to(el, {
              backgroundPosition: directionMultiplier > 0 ? '20px 0' : '-20px 0',
              duration: 2,
              repeat: -1,
              ease: 'linear'
            })
          })
        })
      }
    }
  }
}
</script>

<style scoped>
.request-flow-visualization {
  margin: 20px 0;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.flow-container {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 20px;
  width: 100%;
}

.node {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 100px;
  height: 100px;
  border-radius: 10px;
  background-color: #f8f9fa;
  border: 2px solid #dee2e6;
  padding: 10px;
  margin: 0 10px;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
  z-index: 1;
}

.node:hover {
  transform: translateY(-5px);
  box-shadow: 0 6px 8px rgba(0, 0, 0, 0.15);
}

.node-icon {
  margin-bottom: 10px;
  color: #6c757d;
}

.node-icon i {
  width: 24px;
  height: 24px;
}

.node-label {
  font-size: 0.85rem;
  font-weight: 500;
  text-align: center;
  color: #495057;
}

.client-node {
  background-color: #e3f2fd;
  border-color: #90caf9;
}

.interceptor-node {
  background-color: #f3e5f5;
  border-color: #ce93d8;
}

.rule-node {
  background-color: #e8f5e9;
  border-color: #a5d6a7;
}

.proxy-node {
  background-color: #fff3e0;
  border-color: #ffcc80;
}

.target-node {
  background-color: #e1f5fe;
  border-color: #81d4fa;
}

.flow-arrow {
  position: relative;
  width: 60px;
  height: 20px;
  margin: 0 -5px;
}

.arrow-line {
  position: absolute;
  top: 9px;
  left: 0;
  width: 100%;
  height: 2px;
  background: repeating-linear-gradient(
    to right,
    #6c757d,
    #6c757d 8px,
    transparent 8px,
    transparent 12px
  );
  background-size: 20px 2px;
  background-position: 0 0;
}

.arrow-head {
  position: absolute;
  right: -1px;
  top: 5px;
  width: 0;
  height: 0;
  border-top: 5px solid transparent;
  border-bottom: 5px solid transparent;
  border-left: 8px solid #6c757d;
}

.return-arrow .arrow-head {
  right: auto;
  left: -1px;
  border-left: none;
  border-right: 8px solid #dc3545;
}

.return-arrow .arrow-line {
  background: repeating-linear-gradient(
    to right,
    #dc3545,
    #dc3545 8px,
    transparent 8px,
    transparent 12px
  );
  background-size: 20px 2px;
}

.rule-arrow .arrow-head {
  border-left-color: #28a745;
}

.rule-arrow .arrow-line {
  background: repeating-linear-gradient(
    to right,
    #28a745,
    #28a745 8px,
    transparent 8px,
    transparent 12px
  );
  background-size: 20px 2px;
}

.flow-legend {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 15px;
  font-size: 0.85rem;
  color: #6c757d;
}

.legend-item {
  display: flex;
  align-items: center;
}

.arrow-sample {
  display: inline-block;
  width: 20px;
  height: 2px;
  margin-right: 5px;
  background-color: #6c757d;
}

.arrow-sample.return {
  background-color: #dc3545;
}

.arrow-sample.rule {
  background-color: #28a745;
}

@media (max-width: 768px) {
  .flow-container {
    flex-direction: column;
  }
  
  .flow-arrow {
    transform: rotate(90deg);
    margin: 10px 0;
  }
  
  .return-arrow {
    transform: rotate(-90deg);
  }
}
</style>