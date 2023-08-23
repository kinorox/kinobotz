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
            showPassword: [],
            UserAccessLevelEnum: {
                Default: 0,
                Vip: 1,
                Subscriber: 2,
                Moderator: 3,
                Broadcaster: 4
            }
        };
    },
    name: "DashboardApp",
    components: { NavBar },
    mounted() {
        if (!Cookies.get('jwtToken')) {
            router.push('/')
        } else {
            this.load();
        }
    },
    computed: {
        UserAccessLevelEnumValues() {
            return Object.values(this.UserAccessLevelEnum);
        },
        UserAccessLevelEnumNames() {
            return {
                [this.UserAccessLevelEnum.Default]: 'Everyone',
                [this.UserAccessLevelEnum.Vip]: 'Vip',
                [this.UserAccessLevelEnum.Subscriber]: 'Subscriber',
                [this.UserAccessLevelEnum.Moderator]: 'Moderator',
                [this.UserAccessLevelEnum.Broadcaster]: 'Broadcaster'
            };
        }
    },
    methods: {
        restrictToPositiveIntegers(item, itemKey) {
            const inputValue = item[itemKey];
            const intValue = parseInt(inputValue);
            if (!isNaN(intValue) && intValue >= 0) {
                item[itemKey] = intValue;
            } else {
                item[itemKey] = 0;
            }
        },
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
                        this.showAlert = true;
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                        this.resetForm();
                    })
                    .catch(error => {
                        console.log(error)
                    });
            } catch (error) {
                console.log(error)
            }
        },
        getFieldType(key) {
            if (key === 'email') {
                return 'email';
            } else if (key === 'active' || key === 'globalCooldown' || key === 'enabled' || key === 'custom') {
                return 'checkbox';
            } else if (key === 'discordClipsWebhookUrl' || key === 'discordTtsWebhookUrl') {
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
                || key === 'login'
                || key === 'prefix'
                || key === 'description';
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
    <NavBar />
    <div v-if="showAlert" id="formAlert" class="alert alert-success alert-dismissible fade show" role="alert">
        Saved
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
    <div class="mb-3 form-group">
        <label for="overlayId" class="form-label">Overlay <i>(add this as a Browser Source on OBS)</i></label>
        <div class="d-inline-flex p-2 form-control justify-content-between">
            <input id="overlayId" class="form-control" :type="showPassword['overlayId'] ? 'text' : 'password'"
                v-model="overlay" readonly disabled>
            <button class="btn btn-outline-secondary" type="button" @click="toggleShowPassword('overlayId')">{{
                showPassword['overlayId'] ? 'Hide' : 'Show' }}</button>
            <button class="btn btn-outline-secondary" type="button" @click="copyPassword('overlayId')">Copy</button>
        </div>
    </div>
    <form @submit.prevent="submitForm" class="needs-validation" novalidate>
        <div v-for="(value, key) in formData" :key="key" class="mb-3 form-group">
            <label :for="key" class="form-label">{{ key }}</label>
            <div v-if="getFieldType(key) == 'password'" class="d-inline-flex p-2 form-control justify-content-between">
                <input :id="key" class="form-control" :type="showPassword[key] ? 'text' : 'password'"
                    v-model="formData[key]">
                <button class="btn btn-outline-secondary" type="button" @click="toggleShowPassword(key)">{{
                    showPassword[key] ? 'Hide' : 'Show' }}</button>
                <button class="btn btn-outline-secondary" type="button" @click="copyPassword(key)">Copy</button>
            </div>
            <template v-else-if="key === 'channelCommands'">
                <div>
                    <table class="table">
                        <thead>
                            <tr>
                                <th v-for="(property, propertyName) in value[0]" :key="propertyName">{{ propertyName }}</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="(item, index) in value" :key="index">
                                <td v-for="(itemValue, itemKey) in item" :key="itemKey">
                                    <template v-if="itemKey === 'description' || itemKey === 'prefix'">
                                        {{ itemValue }}
                                    </template>
                                    <template v-else-if="itemKey === 'accessLevel'">
                                        <select class="form-select" v-model="item[itemKey]">
                                            <option v-for="accessLevel in UserAccessLevelEnumValues" :value="accessLevel"
                                                :key="accessLevel">
                                                {{ UserAccessLevelEnumNames[accessLevel] }}
                                            </option>
                                        </select>
                                    </template>
                                    <template v-else-if="itemKey === 'cooldown'">
                                        <input class="form-control" v-model.number="item[itemKey]" type="number"
                                            :class="[getFieldClass(itemKey)]" :required="isRequiredField(itemKey)"
                                            :readonly="isFieldReadOnly(itemKey)" :disabled="isFieldReadOnly(itemKey)"
                                            :min="0" :step="1" @input="restrictToPositiveIntegers(item, itemKey)" />
                                    </template>
                                    <template v-else>
                                        <input class="form-control" v-model="item[itemKey]" :type="getFieldType(itemKey)"
                                            :class="[getFieldClass(itemKey)]" :required="isRequiredField(itemKey)"
                                            :readonly="isFieldReadOnly(itemKey)" :disabled="isFieldReadOnly(itemKey)" />
                                    </template>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </template>
            <input v-else v-model="formData[key]" :type="getFieldType(key)" :class="[getFieldClass(key)]" :id="key"
                :required="isRequiredField(key)" :readonly="isFieldReadOnly(key)" :disabled="isFieldReadOnly(key)" />
        </div>
        <button class="btn btn-secondary" type="submit">Save</button>
    </form>
</template>
