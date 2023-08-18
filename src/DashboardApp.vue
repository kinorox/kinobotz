<script>
import NavBar from './components/NavBar.vue';
import axiosInstance from '@/scripts/axios-instance';
import Cookies from 'js-cookie';
import router from '@/scripts/router'

export default {
    data() {
        return {
            formData: null, 
            showAlert: false,
            overlay: null,
            showPassword: []
        };
    },
    name: "DashboardApp",
    components: { NavBar },
    mounted() {
        if(!Cookies.get('jwtToken')) {
            router.push('/')
        } else {
            this.load();
        }
    },
    methods: {
        load() {
            axiosInstance.get('/BotConnection/profile')
                .then(response => {
                    this.formData = response.data;
                    this.overlay = window.location.origin + `/overlay/${response.data.id}`
                })
                .catch(error => {
                    console.log(error)
                });
        },
        submitForm() {
            try {
                axiosInstance.put('/BotConnection', this.formData)
                .then(() => {
                    this.load()
                })
                .catch(error => {
                    console.log(error)
                });

                this.showAlert = true;
                window.scrollTo({ top: 0, behavior: 'smooth' });
                this.resetForm();
            } catch (error) {
                console.log(error)
            }            
        },
        getFieldType(key) {
            if (key === 'email') {
                return 'email';
            } else if (key === 'active') {
                return 'checkbox';
            } else if (key === 'discordClipsWebhookUrl' || key === 'discordTtsWebhookUrl' ) {
                return 'password';
            }
            
            return 'text';
        },
        getFieldClass(key) {
            return {
                'is-invalid': this.isFieldInvalid(key),
                'form-control': this.getFieldType(key) != 'checkbox',
                'form-check-input': this.getFieldType(key) == 'checkbox',
            };
        },
        isRequiredField(key) {
            return key === 'id' || key === 'channelId' || key === 'login'; // Customize this as needed
        },
        isFieldInvalid(key) {
            return this.formData[key] && this.formData[key].$error;
        },
        isFieldReadOnly(key) {
            return key === 'id' 
            || key === 'createdAt' 
            || key === 'updatedAt' 
            || key === 'channelId' 
            || key === 'profileImageUrl'
            || key === 'email'
            || key === 'accessToken'
            || key === 'refreshToken'
            || key === 'login';
        },
        isObject(value) {
            return typeof value === 'object' && value !== null && !Array.isArray(value);
        },
        toggleShowPassword(key) {
            this.showPassword[key] = !this.showPassword[key];
        },
        copyPassword(key) {
            const passwordInput = document.getElementById(key);
            if (passwordInput) {
                passwordInput.select();
                navigator.clipboard.writeText(passwordInput.value);
            }            
        }
    }
}
</script>

<template>
    <NavBar/>
    <div v-if="showAlert" id="formAlert" class="alert alert-success alert-dismissible fade show" role="alert"> 
        Saved
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
    <div class="mb-3 form-group">
        <label for="overlayId" class="form-label">Overlay <i>(add this as a Browser Source on OBS)</i></label>
        <div class="d-inline-flex p-2 form-control justify-content-between">
            <input id="overlayId" class="form-control" :type="showPassword['overlayId'] ? 'text' : 'password'" v-model="overlay" readonly disabled>
            <button class="btn btn-outline-secondary" type="button" @click="toggleShowPassword('overlayId')">{{ showPassword['overlayId'] ? 'Hide' : 'Show' }}</button>
            <button class="btn btn-outline-secondary" type="button" @click="copyPassword('overlayId')">Copy</button>
        </div>
    </div>
    <form @submit.prevent="submitForm" class="needs-validation" novalidate>
        <div v-for="(value, key) in formData" :key="key" class="mb-3 form-group">    
            <label :for="key" class="form-label">{{ key }}</label> 
            <div v-if="getFieldType(key) == 'password'" class="d-inline-flex p-2 form-control justify-content-between">
                <input :id="key" class="form-control" :type="showPassword[key] ? 'text' : 'password'" v-model="formData[key]">
                <button class="btn btn-outline-secondary" type="button" @click="toggleShowPassword(key)">{{ showPassword[key] ? 'Hide' : 'Show' }}</button>
                <button class="btn btn-outline-secondary" type="button" @click="copyPassword('overlayId')">Copy</button>
            </div>
            <div v-else-if="isObject(value)">
                <div v-for="(innerValue, innerKey) in value" :key="innerKey" class="mb-2">
                    <label :for="innerKey" class="form-label">{{ innerKey }}</label>
                    <input
                        v-model="formData[key][innerKey]"
                        type='checkbox'
                        class='form-check-input'
                        :id="innerKey"
                        :required="isRequiredField(innerKey)"
                        :readonly="isFieldReadOnly(innerKey)"
                        :disabled="isFieldReadOnly(innerKey)"
                    />
                </div>
            </div> 
            <input v-else 
                v-model="formData[key]"
                :type="getFieldType(key)"
                :class="[getFieldClass(key)]"
                :id="key"
                :required="isRequiredField(key)"
                :readonly="isFieldReadOnly(key)"
                :disabled="isFieldReadOnly(key)"
            />                  
        </div>
    <button class="btn btn-secondary" type="submit">Save</button>
  </form>
</template>
