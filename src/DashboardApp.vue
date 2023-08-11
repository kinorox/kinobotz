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
            showPassword: false
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
        toggleShowPassword() {
            this.showPassword = !this.showPassword;
        },
        copyPassword(key) {
            const passwordInput = document.getElementById(key);
            if (passwordInput) {
                passwordInput.select();
                document.execCommand('copy');
            }            
        },
    }
}
</script>

<template>
    <NavBar/>
    <div v-if="showAlert" id="formAlert" class="alert alert-success alert-dismissible fade show" role="alert"> 
        Saved
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
    <div>
        <label for="overlayId" class="form-label">Overlay</label>
        <input id="overlayId" class="form-control" :type="showPassword ? 'text' : 'password'" v-model="overlay" readonly disabled>
        <button class="btn btn-outline-secondary" type="button" @click="toggleShowPassword">{{ showPassword ? 'Hide' : 'Show' }}</button>
        <button class="btn btn-outline-secondary" type="button" @click="copyPassword(key)">Copy</button>
    </div>
    <form @submit.prevent="submitForm" class="needs-validation" novalidate>
        <div v-for="(value, key) in formData" :key="key" class="mb-3">
        <label :for="key" class="form-label">{{ key }}</label>
        <input
            v-if="!isObject(value)" 
            v-model="formData[key]"
            :type="getFieldType(key)"
            :class="[getFieldClass(key)]"
            :id="key"
            :required="isRequiredField(key)"
            :readonly="isFieldReadOnly(key)"
            :disabled="isFieldReadOnly(key)"
        />
        <div v-else>
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
        </div>
    <button type="submit" class="btn btn-primary">Submit</button>
  </form>
</template>
